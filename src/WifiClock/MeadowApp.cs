using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Sensors.Buttons;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Hardware;
using Meadow.Units;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WifiClock
{
    public class MeadowApp : App<F7CoreComputeV2>
    {
        MicroGraphics graphics;
        private IProjectLabHardware _projLab;

        bool showDate;

        public override async Task Initialize()
        {
            //==== instantiate the project lab hardware
            _projLab = ProjectLab.Create();

            var display = new Max7219(
                spiBus: Device.CreateSpiBus(Device.Pins.SPI5_SCK,
                                            Device.Pins.SPI5_COPI,
                                            Device.Pins.SPI5_CIPO),
                chipSelectPin: Device.Pins.D14,
                deviceCount: 4,
                maxMode: Max7219.Max7219Mode.Display);

            graphics = new MicroGraphics(display)
            {
                CurrentFont = new Font4x8(),
                Rotation = RotationType._180Degrees
            };

            //---- BME688 Atmospheric sensor
            if (_projLab.EnvironmentalSensor is { } bme688)
            {
                bme688.StartUpdating(TimeSpan.FromSeconds(5));
            }

            graphics.Clear();
            graphics.DrawText(0, 1, "WI");
            graphics.DrawText(0, 9, "FI");
            graphics.DrawText(0, 17, "TI");
            graphics.DrawText(0, 25, "ME");
            graphics.Show();

            if (_projLab.DownButton is { } downButton)
            {
                downButton.Clicked += PushButtonClicked;
            }

            var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

            await wifi.Connect(Secrets.WIFI_NAME, Secrets.WIFI_PASSWORD, TimeSpan.FromSeconds(45));

            _ = _projLab.RgbLed?.StartPulse(WildernessLabsColors.PearGreen);
        }

        void PushButtonClicked(object sender, EventArgs e)
        {
            showDate = true;
            Thread.Sleep(5000);
            showDate = false;
        }

        public override Task Run()
        {
            while (true)
            {
                DateTime clock = DateTime.Now.AddHours(+1);

                graphics.Clear();

                graphics.DrawText(0, 1, $"{clock:hh}");
                graphics.DrawText(0, 9, $"{clock:mm}");
                graphics.DrawText(0, 17, $"{clock:ss}");
                graphics.DrawText(0, 25, $"{clock:tt}");

                if (showDate)
                {
                    graphics.Clear();

                    graphics.DrawText(0, 1, $"{clock:dd}");
                    graphics.DrawText(0, 9, $"{clock:MM}");
                    graphics.DrawText(0, 17, $"{clock:yy}");

                    graphics.DrawHorizontalLine(0, 24, 7, true);

                    var temperature = $"{_projLab.EnvironmentalSensor.Conditions.Temperature.Value.Celsius}";

                    graphics.DrawText(0, 26, temperature);
                }

                graphics.Show();
                Thread.Sleep(1000);
            }
        }
 
    }
}