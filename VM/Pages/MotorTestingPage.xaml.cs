using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using VM.Components;
using VM.Domains;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages
{
    public partial class MotorTestingPage : Page
    {
        private readonly AppDbContext dbContext;
        private readonly IModbus modbus;
        private readonly ISensorService sensorService;
        private readonly ILogger<MotorTestingPage> logger;

        private bool pauseAMT = false;
        private DispatcherTimer timer = new DispatcherTimer();

        public MotorTestingPage(AppDbContext dbContext, IModbus modbus, ISensorService sensorService, ILogger<MotorTestingPage> logger)
        {
            InitializeComponent();
            this.dbContext = dbContext;
            this.modbus = modbus;
            this.sensorService = sensorService;
            this.logger = logger;

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            modbus.Open();
            ResetAMT();
        }


        private void btnResetJuicer_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = "Resetting..";

            modbus.ResetJuicer();
            logger.LogInformation("Reset runs");
        }
        private async void btnRunMotor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                lblStatus.Text = string.Empty;

                if (!modbus.IsOpen())
                {
                    DisplayMsg("Modbus open failed");
                    return;
                }

                btnRunMotor.IsEnabled = false;
              
                lblStatus.Text = "Starting.";

                int cupStation = 1;
                if (!modbus.RunMotor(cupStation))
                {
                    DisplayMsg("Modbus Write failed");
                    return;
                }

                bool IsVendSuccess = false;

                bool IsContinue = true;
                int waitCount = 0;
                int processStatus = 0;

                while (IsContinue)
                {
                    await Task.Delay(1000);
                    waitCount++;
                    processStatus = modbus.Status();
                    logger.LogInformation($"Process status: {processStatus}");

                    switch (processStatus)
                    {
                        case 0:
                        case 1:
                            lblStatus.Text = $"Starting{new string('.', waitCount % 4)}";
                            break;
                        case 2:
                            logger.LogInformation("Cup dispensed");
                            lblStatus.Text = $"Processing{new string('.', waitCount % 4)}";
                            break;
                        case 3:
                            logger.LogInformation("Fruit count 4");
                            lblStatus.Text = $"Processin{new string('.', waitCount % 4)}";

                            break;
                        case 4:
                            IsVendSuccess = true;

                            logger.LogInformation("Juicer motor started");
                            lblStatus.Text = $"Crushing Your Juice, Please wait{new string('.', waitCount % 4)}";
                            await Task.Delay(2000);
                            break;
                        case 5:
                            IsVendSuccess = true;
                            IsContinue = false;
                            logger.LogInformation("Cup wrapper");
                            lblStatus.Text = $"Please wait cup wraping{new string('.', waitCount % 4)}";
                            break;
                        case 6:
                            IsVendSuccess = true;
                            IsContinue = false;
                            logger.LogInformation("Cup taken");
                            lblStatus.Text = $"Enjoy your Juice{new string('.', waitCount % 4)}";
                            break;
                           
                        case 21:
                        case 22:
                        case 23:
                            logger.LogInformation("Home position error");
                            break;
                        case 24:
                            IsContinue = false;
                            logger.LogInformation("Cup empty");
                            modbus.ResetJuicer();
                            break;
                      
                        case 25:
                            IsContinue = false;
                            logger.LogInformation("Cup station sensor detection failure");
                            modbus.ResetJuicer();
                            break;
                        //case 26:
                        //    IsContinue = false;
                        //    logger.LogInformation("Cup station delivery point sensor detection failure");
                        //    modbus.ResetJuicer();
                        //    break;
                        case 26:
                            IsContinue = false;
                            logger.LogInformation("Fruit count empty");
                            modbus.ResetJuicer();
                            break;
                        case 27:
                            logger.LogInformation("Juice door open");
                            lblStatus.Text = $"Please close the door{new string('.', waitCount % 4)}";
                            break;
                        case 28:
                            logger.LogInformation("Juice door open and cup taken");
                            lblStatus.Text = $"Please take the cup and close the door{new string('.', waitCount % 4)}";
                            break;
                        case 29:
                            IsContinue = false;
                            logger.LogInformation("Process abort");
                            modbus.ResetJuicer();
                            break;
                        case 30:
                            IsContinue = false;
                            logger.LogInformation("Cup station home not reached.");
                            modbus.ResetJuicer();
                            break;
                        default:
                            logger.LogInformation($"Unknown status code received: {processStatus}");
                            logger.LogInformation($"Modbus status: {modbus.modbusStatus}");
                            break;
                    }

                    if (waitCount >= 180)
                    {
                        IsContinue = false;
                        logger.LogInformation("Time out");
                    }
                }

                btnRunMotor.IsEnabled = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while running motor");
                btnRunMotor.IsEnabled = true;
            }
        }

        private async void btnStatus_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = modbus.modbusStatus;
            logger.LogInformation($"modbusStatus;{modbus.modbusStatus}");
        }

        private void btnPauseAndPlay_Click(object sender, RoutedEventArgs e)
        {
            pauseAMT = !pauseAMT;
            btnPauseAndPlay.Content = pauseAMT ? "PLAY" : "PAUSE";
        }

        private void btnCancelAllMotorTest_Click(object sender, RoutedEventArgs e)
        {
            ResetAMT();
        }

        private void ResetAMT()
        {
          
            pauseAMT = false;
            btnPauseAndPlay.Content = "PAUSE";
            btnCancelAllMotorTest.Visibility = Visibility.Collapsed;
            btnPauseAndPlay.Visibility = Visibility.Collapsed;
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                bool di0 = modbus.GetInstantValue(0);

                bool di1 = modbus.GetInstantValue(1);

                bool di2 = modbus.GetInstantValue(2);

                bool di3 = modbus.GetInstantValue(3);

                bool di4 = modbus.GetInstantValue(4);

                bool di5 = modbus.GetInstantValue(5);

                bool di6 = modbus.GetInstantValue(6);

                bool di7 = modbus.GetInstantValue(7);

                bool di8 = modbus.GetInstantValue(8);

                bool di9 = modbus.GetInstantValue(9);

              



                // Convert Boolean values to "High" or "Low"
                string di0Value = di0 == true ? "High" : "Low";
                string di1Value = di1 == true ? "High" : "Low";
                string di2Value = di2 == true ? "High" : "Low";
                string di3Value = di3 == true ? "High" : "Low";
                string di4Value = di4 == true ? "High" : "Low";
                string di5Value = di5 == true ? "High" : "Low";
                string di6Value = di6 == true ? "High" : "Low";
                string di7Value = di7 == true ? "High" : "Low";
                string di8Value = di8 == true ? "High" : "Low";
                string di9Value = di9 == true ? "High" : "Low";
                


                // Set the formatted values to the TextBlock with proper alignment

                //lblSensor1Value.Text = $"{"DI-Value",-12} {"Status",10}\n\n" +
                //                       $"{"Fruit basket sensor:",-20} {di0Value}\n\n" +
                //                       $"{"Fruit Loading conveyor:",-20} {di1Value}\n\n" +
                //                       $"{"Fruit count limit",-20} {di2Value}\n\n" +
                //                       $"{"Juicer cam Limit",-20} {di3Value}\n\n" +
                //                       $"{"Juicer mechanical limit",-20} {di4Value}\n\n" +
                //                       $"{"Sensor up limit",-20} {di5Value}\n\n" +
                //                       $"{"Down Limit",-20} {di6Value}\n\n" +
                //                       $"{"Delivery Door up",-20} {di7Value}\n\n" +
                //                       $"{"Delivery Door down",-20} {di8Value}\n\n" +
                //                       $"{"Door-10:",-20} {di9Value}\n\n" +
                                      


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            ResetAMT();
            NavigationService.GoBack();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            modbus.Close();
            timer.Stop();
        }

        public async void DisplayMsg(string msg)
        {
            try
            {
                if (DialogHost.IsDialogOpen("pgMotorTestingPageHost"))
                    DialogHost.Close("pgMotorTestingPageHost");

                var sampleMessageDialog = new Dialog { Message = { Text = msg } };
                await DialogHost.Show(sampleMessageDialog, "pgMotorTestingPageHost");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while displaying message dialog");
            }
        }

    
        #region Touch Input
        TextBox txtCurrent;
        #endregion
    }

    
}
