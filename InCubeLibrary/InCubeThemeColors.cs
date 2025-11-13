using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath ;
using System.Xml.Xsl ;
using System.Xml.Serialization.Configuration;
using System.Xml.Serialization;
using System.Data;

namespace InCubeLibrary
{
   public sealed  class  InCubeThemeColors
    {
     //  private static readonly object padlock = new object();
       public DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme CurrentThemeColor = DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Blue;
       public enumShowWin8Form CurrentShowWin8Form = enumShowWin8Form.Show ;
       public enumOrientationViewStyle OrientationViewStyle = enumOrientationViewStyle.Tree ;
      
       
//private InCubeSecurityClass SecuritySession;




      private XmlNode node;
       private static InCubeThemeColors singletoninstance = null;

       public static InCubeThemeColors SingletonInstance
       {
           get
           {
             //  lock (padlock)
               {
                   if (singletoninstance == null)
                   {
                       singletoninstance = new InCubeThemeColors();
                     
                   }
                   return singletoninstance;
               }
           }
       }

       private string  strOperatorID;
        public string setOperatorID
        {
            set
            {
                strOperatorID = value; 
             
            }
            get { return strOperatorID; }
        }

        public enum enumOrientationViewStyle
            {Tree = 0,RibbonBar = 1,}
        private enumOrientationViewStyle _OrientationViewStyle;
        public enumOrientationViewStyle propViewStyle
        {
            set { _OrientationViewStyle = value; }
            get { return _OrientationViewStyle; }
        }



        public enum enumMinMax
        { Minimize = 0, Maximize = 1, Disabled = 2, }
        private enumMinMax _MinMax;
        public enumMinMax propMinMax
          {
              set { _MinMax = value; }
              get { return _MinMax; }
          }

      public enum enumShowWin8Form
         { Show = 0, notShow = 1,}
      private enumShowWin8Form _ShowWin8Form;
      public enumShowWin8Form propShowWin8Form
      {
          set { _ShowWin8Form = value; }
          get { return _ShowWin8Form; }
      }






      ///=================================xml variables 
       string strPreString = "DocumentElement/Style[EmployeeID=" ;

       XmlDocument xmlStyleConfig = new XmlDocument();
       string DocumentName = "StyleConfig.xml";
     //  string DataSourcesFile = "\\" + DocumentName;
      //===================================




        public DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme GetCurrentThemeColor()
        {
            return CurrentThemeColor;
        }
///System.Drawing.Color
        public System.Drawing.Color lightcolor()
        {
            DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme MainColor = GetCurrentThemeColor();
            System.Drawing.Color clLightColor;
            switch (MainColor)
            {
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Black:
                    clLightColor = System.Drawing.Color.FromArgb(242, 242, 242);
                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Blue :
                    //  clLightColor = System.Drawing.Color.FromArgb(194, 217, 247); 
                    clLightColor = System.Drawing.Color.FromArgb(240, 244, 255); //very ligt blue
                 
                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Silver :
                    clLightColor = System.Drawing.Color.FromArgb(235, 235, 235);
                    break;
                default :
                    clLightColor = System.Drawing.Color.FromArgb(240, 244, 255); //very ligt blue
                    break;
            }
            //  color = clLightColor;
          return clLightColor;
        }


        public System.Drawing.Color MidColor()
        {
            DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme MainColor = GetCurrentThemeColor();
            System.Drawing.Color clMidColor;
            switch (MainColor)
            {
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Black:
                    clMidColor = System.Drawing.Color.FromArgb(230, 230, 230);
                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Blue:
                    clMidColor = System.Drawing.Color.FromArgb(220, 230, 245);

                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Silver:
                    clMidColor = System.Drawing.Color.FromArgb(235, 235, 235);
                    break;
                default:
                    clMidColor = System.Drawing.Color.FromArgb(220, 230, 245);
                    break;
            }
           //  color = clMidColor;
           return clMidColor;
        }


        public System.Drawing.Color HotColor()
        {
            DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme MainColor = GetCurrentThemeColor();
            System.Drawing.Color clHotColor;
            switch (MainColor)
            {
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Black:
                    clHotColor = System.Drawing.Color.DimGray;
                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Blue:
                    clHotColor = System.Drawing.Color.SteelBlue;

                    break;
                case DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Silver:
                    clHotColor = System.Drawing.Color.Gray;
                    break;
                default:
                    clHotColor = System.Drawing.Color.SteelBlue;
                    break;
            }
           //  color = clHotColor;
           return clHotColor;
        }


       ///-------------------------------A
       private void  createNotExistedXMLDocument()
       {
           if (!(File.Exists(System.Environment.CurrentDirectory + "\\" + DocumentName)))
           {
               XmlTextWriter writer = new XmlTextWriter(System.Environment.CurrentDirectory + "\\" + DocumentName, System.Text.Encoding.UTF8);
               writer.WriteStartDocument(true);
               writer.Formatting = Formatting.Indented;
               writer.Indentation = 2;
               writer.WriteStartElement("DocumentElement");
               writer.WriteEndElement();
               writer.WriteEndDocument();
               writer.Close();
           }

       }
       ///--------------------------------B
       private void LoadXMLDocument()
       {
           //     xmlDoc.Load(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + DataSourcesFile);
           xmlStyleConfig.Load(System.Environment.CurrentDirectory + "\\" + DocumentName);


           XmlNode LoadedSelectedColor = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor");//------2
           XmlNode LoadedShowWin8Form = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ShowWin8Form");//--------3
           XmlNode LoadedMinMax = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "MinMax");//--------------------4
           XmlNode LoadedViewStyle = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ViewStyle");//--------------5


           CurrentThemeColor = (DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme)Enum.Parse(typeof(DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme), LoadedSelectedColor.InnerText.ToString());
           _OrientationViewStyle = (enumOrientationViewStyle)Enum.Parse(typeof(enumOrientationViewStyle), LoadedViewStyle.InnerText.ToString());
           _MinMax = (enumMinMax)Enum.Parse(typeof(enumMinMax), LoadedMinMax.InnerText.ToString());
           _ShowWin8Form = (enumShowWin8Form)Enum.Parse(typeof(enumShowWin8Form), LoadedShowWin8Form.InnerText.ToString());
       


           //CurrentThemeColor = (DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme)Enum.Parse(typeof(DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme), xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor").ToString());
           //   _ViewStyle = (enumViewStyle)Enum.Parse(typeof(enumViewStyle),xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ViewStyle").ToString());
           //   _MinMax = (enumMinMax)Enum.Parse(typeof(enumMinMax), xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "MinMax").ToString());
           //   _ShowWin8Form = (enumShowWin8Form)Enum.Parse(typeof(enumShowWin8Form), xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ShowWin8Form").ToString());
       }
       ///---------------------------------C
       private void CreateDefaultElement()
       {
           XmlElement StyleElem = xmlStyleConfig.CreateElement("Style");//---------------0
           XmlElement EmployeeID = xmlStyleConfig.CreateElement("EmployeeID");//---------1
           XmlElement SelectedColor = xmlStyleConfig.CreateElement("SelectedColor");//---2
           XmlElement ShowWin8Form = xmlStyleConfig.CreateElement("ShowWin8Form");//-----3
           XmlElement MinMax = xmlStyleConfig.CreateElement("MinMax");//-----------------4
           XmlElement ViewStyle = xmlStyleConfig.CreateElement("ViewStyle");//-----------5

           EmployeeID.InnerText = strOperatorID;
           SelectedColor.InnerText = DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme.Blue.ToString();//--------2
           ShowWin8Form.InnerText = _ShowWin8Form.ToString();//---------3
           MinMax.InnerText = _MinMax.ToString();//---------------------4
           ViewStyle.InnerText = _OrientationViewStyle.ToString();//---------------5


           StyleElem.AppendChild(EmployeeID);//---------1
           StyleElem.AppendChild(SelectedColor);//------2
           StyleElem.AppendChild(ShowWin8Form);//-------3
           StyleElem.AppendChild(MinMax);//-------------4
           StyleElem.AppendChild(ViewStyle);//----------5

           xmlStyleConfig.DocumentElement.AppendChild(StyleElem);

           //  Console.WriteLine("Display the modified XML...");
           xmlStyleConfig.Save(System.Environment.CurrentDirectory + "\\" + DocumentName);
       }

       public void LoadeXML()
       {
           //----------------------create the document if it not exist ---------------------
           createNotExistedXMLDocument();     

           //-------------------------------load the document ----------------------------
           LoadXMLDocument();
           //------------------------------Create Element if it not Exist----------
           node = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor");
           if (node == null)
           {
               ///////----------create element ------------------
               CreateDefaultElement();
               // Create a new element and add it to the document.
            
           }
      

        
        }


       public void UpdateXML()
       {
         //  LoadeXML();
           ///-------------------------------load the document ----------------------------
          
           updateElement();

       }

           public void UpdateXML( InCubeThemeColors.enumMinMax UpdateMinMax)
       {
           _MinMax = UpdateMinMax;
           updateElement();

       }

           public void UpdateXML(InCubeThemeColors.enumShowWin8Form UpdateShowWin8Form)
          {
              _ShowWin8Form = UpdateShowWin8Form;
              updateElement();

          }

           public void UpdateXML(InCubeThemeColors.enumOrientationViewStyle UpdateViewStyle)
          {
              _OrientationViewStyle = UpdateViewStyle;
              updateElement();

          }


         

       private void updateElement()
       {
           node = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor");
           if (node == null)
           {
             ///----------create element ------------------
               CreateDefaultElement();
               node = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor");
               /// Create a new element and add it to the document.

           }
           XmlNode SelectedColor = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "SelectedColor");//------2
           XmlNode ShowWin8Form = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ShowWin8Form");//--------3
           XmlNode MinMax = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "MinMax");//--------------------4
           XmlNode ViewStyle = xmlStyleConfig.SelectSingleNode(strPreString + strOperatorID + "]/" + "ViewStyle");//--------------5

          ///----------update  element ------------------
           SelectedColor.InnerText = CurrentThemeColor.ToString();//----2
           ShowWin8Form.InnerText = _ShowWin8Form.ToString();//---------3
           MinMax.InnerText = _MinMax.ToString();//---------------------4
           ViewStyle.InnerText = _OrientationViewStyle.ToString();//---------------5
           xmlStyleConfig.Save(System.Environment.CurrentDirectory + "\\" + DocumentName);
       }


        public void SetCurrentThemeColor(DevComponents.DotNetBar.Rendering.eOffice2007ColorScheme ThemeColor)
        {
          CurrentThemeColor = ThemeColor;
          UpdateXML();
       }

        public void SetCurrentWin8(enumShowWin8Form ShowWin8Form)
        {
            CurrentShowWin8Form = ShowWin8Form;
            UpdateXML(CurrentShowWin8Form);
        }


        public void SetOrientation(enumOrientationViewStyle OrientationStyle)
        {
            OrientationViewStyle = OrientationStyle;
            UpdateXML(OrientationViewStyle);
        }
      


   }

 
}
