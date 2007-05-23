// Copyright (c) 2007 Michael Chapman
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Globalization;
using System.Windows.Forms;

namespace FlexFieldControlLib
{
   internal class HexadecimalValue : IValueFormatter
   {
      public virtual int MaxFieldLength
      {
         get
         {
            return String.Format( CultureInfo.InvariantCulture, "{0:x}", int.MaxValue ).Length - 1;
         }
      }

      public virtual int MaxValue( int fieldLength )
      {
         int result;

         fieldLength = Math.Min( fieldLength, MaxFieldLength );
         string valueString = new String( 'f', fieldLength );

         Int32.TryParse( valueString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result );

         return result;
      }

      public virtual int Value( string text )
      {
         if ( text == null )
         {
            return 0;
         }

         int result;

         Int32.TryParse( text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result );

         return result;
      }

      public virtual string ValueText( int value )
      {
         return String.Format( CultureInfo.InvariantCulture, "{0:x}", value );
      }

      public virtual bool IsValidKey( KeyEventArgs e )
      {
         if ( e.KeyCode < Keys.A || e.KeyCode > Keys.F )
         {
            if ( e.KeyCode < Keys.NumPad0 || e.KeyCode > Keys.NumPad9 )
            {
               if ( e.KeyCode < Keys.D0 || e.KeyCode > Keys.D9 )
               {
                  return false;
               }
            }
         }

         return true;
      }
   }
}