using PlayLogger.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace PlayLogger.Wpf
{
    public class RememberWindowSizeAndPositionBehavior : Behavior<Window>
    {
        private Window m_TheWindow;
        protected override void OnAttached()
        {
            base.OnAttached();
            m_TheWindow = AssociatedObject;

            loadSettings();
            attachClosingEventHandler();
        }

        private void attachClosingEventHandler()
        {
            m_TheWindow.Closing += (o, e) =>
            {
                saveSettings();
            };
        }

        private void saveSettings()
        {
            WindowLocationSettings settings;
            if (m_TheWindow.WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                settings = new WindowLocationSettings()
                {
                    Top = m_TheWindow.RestoreBounds.Top,
                    Left = m_TheWindow.RestoreBounds.Left,
                    Height = m_TheWindow.RestoreBounds.Height,
                    Width = m_TheWindow.RestoreBounds.Width,
                    Maximized = true
                };
            }
            else
            {
                settings = new WindowLocationSettings()
                {
                    Top = m_TheWindow.Top,
                    Left = m_TheWindow.Left,
                    Height = m_TheWindow.Height,
                    Width = m_TheWindow.Width,
                    Maximized = false
                };
            }

            UserSettings.Set(ExtensionMethods.GetPropertyName(() => Properties.Settings.Default.LastWindowLoc), settings);
        }

        private void loadSettings()
        {
            var winSettings = (WindowLocationSettings)UserSettings.Get(ExtensionMethods.GetPropertyName(() => Properties.Settings.Default.LastWindowLoc));
            if (winSettings != null)
            {
                m_TheWindow.Top = winSettings.Top;
                m_TheWindow.Left = winSettings.Left;
                m_TheWindow.Height = winSettings.Height;
                m_TheWindow.Width = winSettings.Width;
                if (winSettings.Maximized)
                {
                    m_TheWindow.WindowState = WindowState.Maximized;
                }
            }
        }
    }
}
