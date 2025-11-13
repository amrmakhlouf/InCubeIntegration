using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
namespace InCubeLibrary
{
    public class TextboxRdlGenerator
    {
        public static int TextBoxCounters = 0;  // the first method 
        public static int TextBoxCounters1 = 0; // for the overloaded method 
        public static int textboxesArrayCount;
        public static object[] TextboxeValue;
        public static void InitialTextboxeValueSize()
        {
            TextboxeValue = new object[textboxesArrayCount];
        }
        public Rdl.TextboxType CreateTextboxes(bool canGrow, string top, string StyletextAlign, string StylefontSize, string Stylecolor)
        {
            Rdl.TextboxType txt = new Rdl.TextboxType();
            txt.Name = "txt" + TextBoxCounters;
            txt.Items = new object[]
          {
              TextboxsStyle(StyletextAlign , StylefontSize , Stylecolor ),  // style 
              canGrow,
              TextboxeValue[TextBoxCounters],
              top,
          };
            txt.ItemsElementName = new Rdl.ItemsChoiceType14[]
           {
                Rdl.ItemsChoiceType14.Style,
                Rdl.ItemsChoiceType14.CanGrow , 
                Rdl.ItemsChoiceType14.Value , 
                Rdl.ItemsChoiceType14.Top,
              
            };
            TextBoxCounters++;
            return txt;
        }
        public Rdl.TextboxType CreateTextboxes(bool canGrow, object value, string top, string StyletextAlign, string StylefontSize, string Stylecolor)
        {
            Rdl.TextboxType txt = new Rdl.TextboxType();
            txt.Name = "text" + TextBoxCounters1;
            txt.Items = new object[]
          {
              TextboxsStyle(StyletextAlign  , StylefontSize , Stylecolor ),  // style 
              canGrow,
              value,
              top,

          };
            txt.ItemsElementName = new Rdl.ItemsChoiceType14[]
           {
                Rdl.ItemsChoiceType14.Style,
                Rdl.ItemsChoiceType14.CanGrow , 
                Rdl.ItemsChoiceType14.Value ,
                Rdl.ItemsChoiceType14.Top,
               
            };

            TextBoxCounters1++;
            return txt;
        }
        private Rdl.StyleType TextboxsStyle(string textAlign, string fontSize, string color)
        {
            Rdl.StyleType TextboxStyle = new Rdl.StyleType();
            TextboxStyle.Items = new object[]
                {
                    textAlign , 
                    fontSize,
                    color, 
                };
            TextboxStyle.ItemsElementName = new Rdl.ItemsChoiceType5[]
                { 
                    Rdl.ItemsChoiceType5.TextAlign,
                    Rdl.ItemsChoiceType5.FontSize,
                    Rdl.ItemsChoiceType5.Color , 
                   
                };
            return TextboxStyle;
        }
    }
}
