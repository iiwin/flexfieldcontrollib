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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace FlexFieldControlLib
{
   internal class FieldControl : TextBox
   {
      #region Public Events

      public event EventHandler<CedeFocusEventArgs> CedeFocusEvent;
      public event EventHandler<FieldChangedEventArgs> FieldChangedEvent;
      public event EventHandler<EventArgs> FieldSizeChangedEvent;
      public event EventHandler<FieldValidatedEventArgs> FieldValidatedEvent;

      #endregion  // Public Events

      #region Public Properties

      public bool Blank
      {
         get
         {
            return ( Text.Length == 0 );
         }
      }

      public int FieldId
      {
         get
         {
            return _fieldId;
         }
         set
         {
            _fieldId = value;
         }
      }

      public bool LeadingZeros
      {
         get
         {
            return _leadingZeros;
         }
         set
         {
            _leadingZeros = value;

            if ( !Blank )
            {
               Text = GetPaddedText();
            }
         }
      }

      public override int MaxLength
      {
         get
         {
            return base.MaxLength;
         }
         set
         {
            if ( value > _valueFormatter.MaxFieldLength )
            {
               value = _valueFormatter.MaxFieldLength;
            }

            base.MaxLength = value;

            ResetProperties();
         }
      }

      public new Size MinimumSize
      {
         get
         {
            return CalculateMinimumSize();
         }
      }

      public int RangeLow
      {
         get
         {
            return _rangeLow;
         }
         set
         {
            if ( value < 0 )
            {
               _rangeLow = 0;
            }
            else if ( value > RangeHigh )
            {
               _rangeLow = RangeHigh;
            }
            else
            {
               _rangeLow = value;
            }

            if ( _valueFormatter.Value( Text ) < _rangeLow )
            {
               Text = _valueFormatter.ValueText( _rangeLow );
            }
         }
      }

      public int RangeHigh
      {
         get
         {
            return _rangeHigh;
         }
         set
         {
            if ( value < RangeLow )
            {
               _rangeHigh = RangeLow;
            }
            else if ( value > _valueFormatter.MaxValue( MaxLength ) )
            {
               _rangeHigh = _valueFormatter.MaxValue( MaxLength );
            }
            else
            {
               _rangeHigh = value;
            }

            if ( _valueFormatter.Value( Text ) > _rangeHigh )
            {
               Text = _valueFormatter.ValueText( _rangeHigh );
            }
         }
      }

      public int Value
      {
         get
         {
            return _valueFormatter.Value( Text );
         }
         set
         {
            Text = _valueFormatter.ValueText( value );
         }
      }

      public ValueFormat ValueFormat
      {
         get
         {
            return _valueFormat;
         }
         set
         {
            if ( _valueFormat != value )
            {
               _valueFormat = value;

               switch ( _valueFormat )
               {
                  case ValueFormat.Decimal:
                     _valueFormatter = new DecimalValue();
                     break;

                  case ValueFormat.Hexadecimal:
                     _valueFormatter = new HexadecimalValue();
                     break;
               }

               ResetProperties();

               ResetCedeFocusKeys();
            }
         }
      }

      #endregion     // Public Properties

      #region Public Methods
      
      public bool AddCedeFocusKey( KeyEventArgs e )
      {
         if ( _valueFormatter.IsValidKey( e ) )
         {
            return false;
         }

         if ( !_cedeFocusKeys.Contains( e ) )
         {
            _cedeFocusKeys.Add( e );

            return true;
         }

         return false;
      }
  
      public void ClearCedeFocusKeys()
      {
         _cedeFocusKeys.Clear();
      }

      public void ResetCedeFocusKeys()
      {
         ClearCedeFocusKeys();

         KeyEventArgs e = new KeyEventArgs( Keys.Space );
         _cedeFocusKeys.Add( e );
      }

      public void TakeFocus( Direction direction, Selection selection, Action action )
      {
         Focus();

         switch ( action )
         {
            case Action.Trim:

               if ( TextLength > 0 )
               {
                  int newLength = TextLength - 1;
                  Text = Text.Substring( 0, newLength );
               }

               SelectionStart = TextLength;

               return;

            case Action.Home:

               SelectionStart = 0;

               return;

            case Action.End:

               SelectionStart = TextLength;

               return;
         }

         if ( selection == Selection.All )
         {
            SelectionStart = 0;
            SelectionLength = TextLength;
         }
         else
         {
            if ( direction == Direction.Forward )
            {
               SelectionStart = 0;
            }
            else
            {
               SelectionStart = TextLength;
            }
         }
      }

      public override string ToString()
      {
         if ( LeadingZeros )
         {
            return GetPaddedText();
         }

         return _valueFormatter.ValueText( Value );
      }

      #endregion // Public Methods

      #region Constructor

      public FieldControl()
      {
         BorderStyle = BorderStyle.None;
         MaxLength = 3;
         Size = MinimumSize;
         TabStop = false;
         TextAlign = HorizontalAlignment.Center;

         ResetCedeFocusKeys();
      }

      #endregion     // Constructor

      #region Protected Methods

      protected override void OnFontChanged( EventArgs e )
      {
         base.OnFontChanged( e );

         Size = MinimumSize;
      }

      protected override void OnKeyDown( KeyEventArgs e )
      {
         base.OnKeyDown( e );

         switch ( e.KeyCode )
         {
            case Keys.Home:
               SendCedeFocusEvent( Action.Home );
               return;

            case Keys.End:
               SendCedeFocusEvent( Action.End );
               return;
         }

         if ( IsCedeFocusKey( e ) )
         {
            SendCedeFocusEvent( Direction.Forward, Selection.All );
            e.SuppressKeyPress = true;
            return;
         }
         else if ( IsForwardKey( e ) )
         {
            if ( e.Modifiers == Keys.Control )
            {
               SendCedeFocusEvent( Direction.Forward, Selection.All );
               return;
            }
            else if ( SelectionLength == 0 && SelectionStart == TextLength )
            {
               SendCedeFocusEvent( Direction.Forward, Selection.None );
               return;
            }
         }
         else if ( IsReverseKey( e ) )
         {
            if ( e.Modifiers == Keys.Control )
            {
               SendCedeFocusEvent( Direction.Reverse, Selection.All );
               return;
            }
            else if ( SelectionLength == 0 && SelectionStart == 0 )
            {
               SendCedeFocusEvent( Direction.Reverse, Selection.None );
               return;
            }
         }
         else if ( IsBackspaceKey( e ) )
         {
            HandleBackspaceKey( e );
         }
         else if ( !_valueFormatter.IsValidKey( e ) &&
                   !IsEditKey( e ) )
         {
            e.SuppressKeyPress = true;
         }
      }

      protected override void OnSizeChanged( EventArgs e )
      {
         base.OnSizeChanged( e );

         if ( FieldSizeChangedEvent != null )
         {
            FieldSizeChangedEvent( this, EventArgs.Empty );
         }
      }

      protected override void OnTextChanged( EventArgs e )
      {
         base.OnTextChanged( e );

         if ( !Blank )
         {
            int value = _valueFormatter.Value( Text );

            if ( value > RangeHigh )
            {
               base.Text = _valueFormatter.ValueText( RangeHigh );
               SelectionStart = 0;
            }
            else
            {
               int originalLength = TextLength;
               int newSelectionStart = SelectionStart;

               base.Text = GetCasedText();

               if ( TextLength < originalLength )
               {
                  newSelectionStart -= ( originalLength - TextLength );
                  SelectionStart = newSelectionStart;
               }
            }
         }

         SendFieldChangedEvent();

         if ( Text.Length == MaxLength && Focused && SelectionStart == TextLength )
         {
            SendCedeFocusEvent( Direction.Forward, Selection.All );
         }
      }

      protected override void OnValidating( System.ComponentModel.CancelEventArgs e )
      {
         base.OnValidating( e );

         if ( !Blank )
         {
            if ( Value < RangeLow )
            {
               Text = RangeLow.ToString( CultureInfo.InvariantCulture );
            }

            if ( LeadingZeros )
            {
               Text = GetPaddedText();
            }
         }
      }

      protected override void OnValidated( EventArgs e )
      {
         base.OnValidated( e );

         if ( FieldValidatedEvent != null )
         {
            FieldValidatedEventArgs args = new FieldValidatedEventArgs();

            args.FieldId = FieldId;
            args.Text = Text;

            FieldValidatedEvent( this, args );
         }
      }

      #endregion     // Protected Methods

      #region Private Methods

      private Size CalcMinSizeHexadecimal()
      {
         Size minSize = new Size( 0, 0 );

         if ( CharacterCasing == CharacterCasing.Lower )
         {
            for ( char c = 'a'; c <= 'f'; ++c )
            {
               Size newSize = TextRenderer.MeasureText( new string( c, MeasureCharCount ), Font );

               newSize.Width = (int)Math.Ceiling( (double)newSize.Width / (double)MeasureCharCount );

               if ( newSize.Width > minSize.Width )
               {
                  minSize.Width = newSize.Width;
               }

               if ( newSize.Height > minSize.Height )
               {
                  minSize.Height = newSize.Height;
               }
            }
         }
         else
         {
            for ( char c = 'A'; c <= 'F'; ++c )
            {
               Size newSize = TextRenderer.MeasureText( new string( c, MeasureCharCount ), Font );

               newSize.Width = (int)Math.Ceiling( (double)newSize.Width / (double)MeasureCharCount );

               if ( newSize.Width > minSize.Width )
               {
                  minSize.Width = newSize.Width;
               }

               if ( newSize.Height > minSize.Height )
               {
                  minSize.Height = newSize.Height;
               }
            }
         }

         minSize.Width *= MaxLength;

         Size minNumericSize = CalcMinSizeDecimal();

         if ( minNumericSize.Width > minSize.Width )
         {
            minSize.Width = minNumericSize.Width;
         }

         if ( minNumericSize.Height > minSize.Height )
         {
            minSize.Height = minNumericSize.Height;
         }

         return minSize;
      }

      private Size CalcMinSizeDecimal()
      {
         Size minSize = new Size( 0, 0 );

         for ( char c = '0'; c <= '9'; ++c )
         {
            Size newSize = TextRenderer.MeasureText( new string( c, MeasureCharCount ), Font );

            newSize.Width = (int)Math.Ceiling( (double)newSize.Width / (double)MeasureCharCount );

            if ( newSize.Width > minSize.Width )
            {
               minSize.Width = newSize.Width;
            }

            if ( newSize.Height > minSize.Height )
            {
               minSize.Height = newSize.Height;
            }
         }

         minSize.Width *= MaxLength;

         return minSize;
      }

      private Size CalculateMinimumSize()
      {
         switch ( ValueFormat )
         {
            case ValueFormat.Decimal:
               return CalcMinSizeDecimal();
            case ValueFormat.Hexadecimal:
               return CalcMinSizeHexadecimal();
            default:
               return new Size( 0, 0 );
         }
      }

      private string GetCasedText()
      {
         string text = Text;

         int value = _valueFormatter.Value( Text );

         string valueString = _valueFormatter.ValueText( value );

         int textIndex = 0;
         int valueStringIndex = 0;

         StringBuilder sb = new StringBuilder();

         bool nonZeroFound = false;

         bool zeroAppended = false;

         while ( ( textIndex < text.Length ) &&
                 ( valueStringIndex < valueString.Length ) )
         {
            if ( !nonZeroFound && text[textIndex] == '0' )
            {
               if ( LeadingZeros )
               {
                  sb.Append( '0' );
               }
               else if ( !zeroAppended && ( value == 0 ) )
               {
                  sb.Append( '0' );
                  zeroAppended = true;
               }

               ++textIndex;          
            }
            else if ( text[textIndex] == valueString[valueStringIndex] )
            {
               sb.Append( valueString[valueStringIndex] );

               ++textIndex;
               ++valueStringIndex;

               nonZeroFound = true;
            }
            else if ( Char.IsUpper( text[textIndex] ) )
            {
               sb.Append( Char.ToUpper( valueString[valueStringIndex], CultureInfo.InvariantCulture ) );

               ++textIndex;
               ++valueStringIndex;

               nonZeroFound = true;
            }
            else
            {
               ++textIndex;
            }
         }

         return sb.ToString();
      }

      private string GetPaddedText()
      {
         StringBuilder sb = new StringBuilder( GetCasedText() );

         while ( sb.Length < MaxLength )
         {
            sb.Insert( 0, '0' );
         }

         return sb.ToString();
      }

      private void HandleBackspaceKey( KeyEventArgs e )
      {
         if ( TextLength == 0 || ( SelectionStart == 0 && SelectionLength == 0 ) )
         {
            SendCedeFocusEvent( Action.Trim );
            e.SuppressKeyPress = true;
         }
      }

      private static bool IsBackspaceKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Back )
         {
            return true;
         }

         return false;
      }

      private bool IsCedeFocusKey( KeyEventArgs e )
      {
         bool isCedeFocusKey = false;

         foreach ( KeyEventArgs key in _cedeFocusKeys )
         {
            if ( e.KeyCode == key.KeyCode )
            {
               isCedeFocusKey = true;
               break;
            }
         }

         if ( isCedeFocusKey )
         {
            if ( TextLength != 0 && SelectionLength == 0 && SelectionStart != 0 )
            {
               return true;
            }
         }

         return false;
      }

      private static bool IsEditKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Back ||
              e.KeyCode == Keys.Delete )
         {
            return true;
         }
         else if ( e.Modifiers == Keys.Control &&
                  ( e.KeyCode == Keys.C ||
                    e.KeyCode == Keys.V ||
                    e.KeyCode == Keys.X ) )
         {
            return true;
         }

         return false;
      }

      private static bool IsForwardKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Right ||
              e.KeyCode == Keys.Down )
         {
            return true;
         }

         return false;
      }

      private static bool IsReverseKey( KeyEventArgs e )
      {
         if ( e.KeyCode == Keys.Left ||
              e.KeyCode == Keys.Up )
         {
            return true;
         }

         return false;
      }

      private void ResetProperties()
      {
         RangeLow = 0;
         RangeHigh = _valueFormatter.MaxValue( base.MaxLength );

         Text = String.Empty;

         Size = MinimumSize;
      }

      private void SendCedeFocusEvent( Action action )
      {
         if ( CedeFocusEvent != null )
         {
            CedeFocusEventArgs args = new CedeFocusEventArgs();

            args.Action = action;
            args.FieldId = FieldId;

            CedeFocusEvent( this, args );
         }
      }

      private void SendCedeFocusEvent( Direction direction, Selection selection )
      {
         if ( CedeFocusEvent != null )
         {
            CedeFocusEventArgs args = new CedeFocusEventArgs();

            args.Action = Action.None;
            args.Direction = direction;
            args.FieldId = FieldId;
            args.Selection = selection;

            CedeFocusEvent( this, args );
         }
      }

      private void SendFieldChangedEvent()
      {
         if ( FieldChangedEvent != null )
         {
            FieldChangedEventArgs args = new FieldChangedEventArgs();
            args.FieldId = FieldId;
            args.Text = Text;
            FieldChangedEvent( this, args );
         }
      }

      #endregion     // Private Methods

      #region Private Constants

      private const int MeasureCharCount = 10;

      #endregion     // Private Constants

      #region Private Data

      private int _fieldId = -1;
      private ValueFormat _valueFormat = ValueFormat.Decimal;
      private IValueFormatter _valueFormatter = new DecimalValue();
      private bool _leadingZeros;

      private int _rangeLow;  // = 0
      private int _rangeHigh = Int32.MaxValue;

      private List<KeyEventArgs> _cedeFocusKeys = new List<KeyEventArgs>();

      #endregion     // Private Data
   }

   public enum ValueFormat
   {
      Decimal,
      Hexadecimal
   }
}
