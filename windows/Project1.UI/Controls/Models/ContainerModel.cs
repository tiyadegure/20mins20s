using Project1.UI.Cores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Project1.UI.Controls.Models
{
    public class ContainerModel : UINotifyPropertyChanged
    {
        private double Opacity_;
        public double Opacity
        {
            get
            {
                return Opacity_;
            }
            set
            {
                Opacity_ = value;
                OnPropertyChanged();
            }
        }
        private Brush Background_;
        public Brush Background
        {
            get
            {

                return Background_;
            }
            set
            {
                Background_ = value;
                OnPropertyChanged();
            }
        }

        private double CenterPanelWidth_;
        public double CenterPanelWidth
        {
            get
            {
                return CenterPanelWidth_;
            }
            set
            {
                CenterPanelWidth_ = value;
                OnPropertyChanged();
            }
        }

        private double CenterPanelHeight_;
        public double CenterPanelHeight
        {
            get
            {
                return CenterPanelHeight_;
            }
            set
            {
                CenterPanelHeight_ = value;
                OnPropertyChanged();
            }
        }

        private double CenterPanelOpacity_;
        public double CenterPanelOpacity
        {
            get
            {
                return CenterPanelOpacity_;
            }
            set
            {
                CenterPanelOpacity_ = value;
                OnPropertyChanged();
            }
        }

        private double CenterPanelCornerRadius_;
        public double CenterPanelCornerRadius
        {
            get
            {
                return CenterPanelCornerRadius_;
            }
            set
            {
                CenterPanelCornerRadius_ = value;
                OnPropertyChanged();
            }
        }

        private double CenterPanelBorderThickness_;
        public double CenterPanelBorderThickness
        {
            get
            {
                return CenterPanelBorderThickness_;
            }
            set
            {
                CenterPanelBorderThickness_ = value;
                OnPropertyChanged();
            }
        }

        private Brush CenterPanelBackground_;
        public Brush CenterPanelBackground
        {
            get
            {
                return CenterPanelBackground_;
            }
            set
            {
                CenterPanelBackground_ = value;
                OnPropertyChanged();
            }
        }

        private Brush CenterPanelBorderBrush_;
        public Brush CenterPanelBorderBrush
        {
            get
            {
                return CenterPanelBorderBrush_;
            }
            set
            {
                CenterPanelBorderBrush_ = value;
                OnPropertyChanged();
            }
        }
    }
}
