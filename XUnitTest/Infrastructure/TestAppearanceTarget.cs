using DevExpress.Drawing;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Tests.Infrastructure
{

    public class TestAppearanceTarget : IAppearanceEnabled, IAppearanceVisibility, IAppearanceFormat
    {
        #region IAppearanceEnabled Members
        private bool enabled;
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }



        public void ResetEnabled()
        {
            Enabled = true;
        }


        #endregion
        #region IAppearanceVisibility


        public ViewItemVisibility Visibility { get; set; }


        public void ResetVisibility()
        {
            Visibility = ViewItemVisibility.Show;
        }


        #endregion
        #region IApperanceFormat

        //public FontStyle FontStyle { get; set; }
        public Color FontColor { get; set; }
        public Color BackColor { get; set; }
        public DXFontStyle FontStyle { get; set; }
   

        public void ResetFontStyle()
        {
            FontStyle = DXFontStyle.Regular;
        }

        public void ResetFontColor()
        {
            FontColor = Color.Black;
        }

        public void ResetBackColor()
        {
            BackColor = Color.Transparent;
        }
        #endregion
    }
}
