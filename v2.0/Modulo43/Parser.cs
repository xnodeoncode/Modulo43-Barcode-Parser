﻿using Modulo43.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Modulo43
{
    #region Overview

    // Parses Code 128 barcode formatted text and validates a value using
    // HIBC Modulo 43 Check Digit calculation.

    // Resources
    // https://en.wikipedia.org/wiki/Code_128
    // http://www.hibcc.org/
    // http://www.hibcc.org/udi-labeling-standards/create-a-bar-code/

    #endregion
        
    public class Parser : IDisposable
    {
        #region Fields

        // Private dictionary repesentation of the Table of Numerical Value Assignments
        // for Computing HIBC LIC data format Check Digit.
        private Dictionary< char,int > _data = new Dictionary<char,int>( );

        // A prefix may be appended to the message. This default is for demonstration purposes
        // as the actual prefix, if it exists, could be different.
        private string _prefix = "";

        // The default modulus 43 based on the standard, however allow for a different
        // modulus to be passed in.
        private int _modulus = 43;

        // An array of possible characters from the HIBC LIC table of values.
        // This is used to populate the dictionary as well as provide character access by index.
        private static readonly char[ ] _chars = new[ ]
        {
            '0','1','2','3','4','5','6','7','8','9','A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', '-', '.', ' ', '$', '/','+', '%'
        };

        #endregion

        #region Properties

        public Dictionary<char, int> Data { get => _data; set => _data = value; }

        public string Prefix { get => _prefix; set => _prefix = value; }

        public int Modulus { get => _modulus; set => _modulus = value; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructory that simply populates the dictionary with array values
        /// to provide key-based access to characters.
        /// </summary>
        public Parser( )
        {
            //build HIBC character dictionary
            BuildDictionary( );
        }

        /// <summary>
        /// Overloaded constructor which accepts an alternate modulus as a parameter for processing the barcode.
        /// </summary>
        /// <param name="modulus"> Integer: Used as a check digit in validating the integrity of an HIBC barcode data. </param>
        public Parser( int modulus )
        {
            Modulus = modulus;

            //build HIBC character dictionary
            BuildDictionary( );
        }

        /// <summary>
        /// Overloaded constructor which accepts an alternate prefix as a parameter
        /// </summary>
        /// <param name="LabelerIdentificationCode">string: Value that is pre-pended to the barcode data that identifies the HIBC registered labeler.</param>
        public Parser( string LabelerIdentificationCode )
        {
            // override default prefix
            Prefix = LabelerIdentificationCode.Trim();

            // build HIBC character dictionary
            BuildDictionary( );
        }

        /// <summary>
        /// Overloaded constructor that accepts an alternate Labeler Identification Code (prefix) and modulus as parameters.
        /// </summary>
        /// <param name="LabelerIdentificationCode"></param>
        /// <param name="modulus"> Integer: Used as a check digit in validating the integrity of an HIBC barcode data. </param>
        public Parser( string LabelerIdentificationCode, int modulus )
        {
            // override default prefix
            Prefix = LabelerIdentificationCode.Trim();

            // override default modulus
            Modulus = modulus;

            //build HIBC character dictionary
            BuildDictionary( );
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Populates the dictionary from the character array.
        /// The position of each character in the character array also represents its 
        /// corresponding value in the HIBC Table of values
        /// </summary>
        private void BuildDictionary( )
        {
            for ( var i = 0; i < _chars.Length; i++ )
            {
                _data.Add( _chars[ i ], i );
            }
        }

        #endregion

        /// <summary>
        /// Returns the HIBC LIC compliant barcode with validation status.
        /// </summary>
        /// <param name="barcodeData">string: The barcode data received.</param>
        /// <returns> Barcode: A barcode object with computed values.</returns>
        public Barcode Parse( string barcodeData )
        {
            // barcode data must not be null or empty.
            if ( string.IsNullOrEmpty( barcodeData ) || string.IsNullOrWhiteSpace( barcodeData ) )
                throw new ArgumentNullException( barcodeData, "{0} cannot be empty." );

            // Strip any leading and trailing whitespace.
            // TODO: if a barcode contains a valid whitespace character it should be allowed.
            // As of now, the barcode will fail.
            barcodeData = barcodeData.Trim( );

            // Validate the number of characters is no more that the HIBC standard of 18
            // HIBC barcodes must not exceed 18 alphanumeric characters.
            if ( barcodeData.Length > 18 )
                throw new FormatException("Invalid format. HIBC barcodes allows up to 18 alphanumeric characters.");

            // Attempt to extract the check digit. Based on HIBC standards
            // the check digit is the last digit in the sequence of characters.
            var checkDigit = barcodeData[ barcodeData.Length - 1 ];

            // Attempt to extract the unit of measure digit. Based on HIBC standards
            // the unit of measure digit is the second from the last digit in the
            // sequence of characters.
            var unitOfMeasure = barcodeData[ barcodeData.Length - 2 ];

            // Cast the string to character array, truncating the leading and trailing spaces
            // along with the check digit character already retrieved.
            // Although the string is already an array of characters, casting it to the official
            // type makes array methods available.
            var characters = barcodeData.ToCharArray( 0, barcodeData.Length - 1 );

            // Extract the prefix
            var prefix = ( !string.IsNullOrEmpty( _prefix ) ) ? barcodeData.Substring( 0, _prefix.Length ) : "";

            // If we've made it this far, we are ready to populate the barcode properties.
            var barcode = new Barcode( );

            // If the prefix is not present, or it doesn't match the set value,
            // then the barcode is invalid.
            if ( !prefix.Equals( _prefix ) )
                throw new Exception("The barcode prefix does not match the expected prefix.");

            // Extract the message
            var itemInformation = barcodeData.Substring(prefix.Length, barcodeData.Length - ( prefix.Length + 2 ) );

            // Calculate the sum of the characters.
            var total = characters.Sum( character => _data[ character ] );

            // Calculate the modulo using the sum of the characters in the barcode data and the constant of 43.
            var modulo = total % Modulus;

            // Retrieve the value of the test digit (expected check digit)
            // from the array of characters above based on the modulo value
            // which should match the index for the corresponding digit.
            var calculatedCheckDigit = _chars[ modulo ];

            // Assign property values to the barcode object and return it as the result.
            barcode.ItemNumber = itemInformation;
            barcode.LabelerIdentificationCode = prefix;
            barcode.CheckDigit = checkDigit;
            barcode.Modulus = modulo;
            barcode.Sum = total;
            barcode.CalculatedCheckDigit = calculatedCheckDigit;
            barcode.IsValid = calculatedCheckDigit.Equals( checkDigit );
            barcode.UnitOfMeasure = unitOfMeasure;
            barcode.Data = barcodeData;
            barcode.TotalNumberOfCharacters = barcodeData.Length;

            return barcode;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Parser() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
