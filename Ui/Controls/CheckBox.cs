using System.Windows;

namespace Ui.Controls
{
    public class CheckBox : System.Windows.Controls.CheckBox
    {
        public static readonly DependencyProperty GlyphDataProperty = DependencyProperty.Register(nameof(GlyphData), typeof(System.Windows.Media.Geometry), typeof(CheckBox));
        static CheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CheckBox), new FrameworkPropertyMetadata(typeof(CheckBox)));
        }

        public System.Windows.Media.Geometry GlyphData
        {
            get { return (System.Windows.Media.Geometry)GetValue(GlyphDataProperty); }
            set { SetValue(GlyphDataProperty, value); }
        }
    }
}
