using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using VM.Components;
using VM.Dtos;
using VM.Services.Interfaces;

namespace VM.Pages;

public partial class ReportsPage : Page
{
    private readonly IReportService reportService;
    private readonly IServerClient httpClient;
    private readonly ILogger<ReportsPage> logger;

    DateTime from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    DateTime to = DateTime.Now;

    public ReportsPage(IReportService reportService, IServerClient httpClient, ILogger<ReportsPage> logger)
    {
        InitializeComponent();
        this.reportService = reportService;
        this.httpClient = httpClient;
        this.logger = logger;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        DateTime from = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        DateTime to = DateTime.Now;

        lblFrom.Text = from.ToString("yyyy-MM-dd HH:mm:ss");
        lblTo.Text = to.ToString("yyyy-MM-dd HH:mm:ss");

        CombinedCalendar.SelectedDate = from.Date;
        CombinedClock.Time = from;

        CombinedCalendar1.SelectedDate = to.Date;
        CombinedClock1.Time = to;
    }

    private async void btnDownload_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DateTime from = DateTime.Parse(lblFrom.Text);
            DateTime to = DateTime.Parse(lblTo.Text);

            if (from >= to)
            {
                DisplayMsg("Invalid date range");
                return;
            }

            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Excel File (*.xls) | *.xls";
            save.DefaultExt = "xls";
            save.AddExtension = true;

            if (save.ShowDialog() == true)
            {              
                var success = await reportService.GenerateExcelReport(from, to, save.FileName);
                if (success)
                    DisplayMsg("Report Downloaded");
                else
                    DisplayMsg("Report generation failed");
            }


        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
        }
    }

    private async void btnEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DateTime from = DateTime.Parse(lblFrom.Text);
            DateTime to = DateTime.Parse(lblTo.Text);

            if (from >= to)
            {
                DisplayMsg("Invalid date range");
                return;
            }

            ShowProgressbar();

            string name = $"ManualReport-{DataStore.MachineInfo.VendorShortName}{DataStore.AppType}{DataStore.MachineInfo.MachineNumber}-{from.ToString("ddMMMyy")}to{to.ToString("ddMMMyy")}.xls";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", name);

            var result = await reportService.EmailReport(from, to, path, "ManualReport");
            DisplayMsg(result.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message, ex);
            CloseDialog();
        }
    }


    private void btnBack_Click(object sender, RoutedEventArgs e)
    {
        NavigationService.GoBack();
    }



    public async void DisplayMsg(string msg)
    {
        try
        {
            if (DialogHost.IsDialogOpen("pgReportsPageHost"))
                DialogHost.Close("pgReportsPageHost");

            var sampleMessageDialog = new Dialog { Message = { Text = msg } };
            await DialogHost.Show(sampleMessageDialog, "pgReportsPageHost");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }


    public async void ShowProgressbar()
    {
        CloseDialog();
        await DialogHost.Show(new ProcessingDialog(), "pgReportsPageHost");
    }

    public void CloseDialog()
    {
        if (DialogHost.IsDialogOpen("pgReportsPageHost"))
            DialogHost.Close("pgReportsPageHost");

    }

    public void CombinedDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
    {
        if (Equals(eventArgs.Parameter, "1") && CombinedCalendar.SelectedDate is DateTime selectedDate)
        {
            from = selectedDate.AddSeconds(CombinedClock.Time.TimeOfDay.TotalSeconds);
            lblFrom.Text = from.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public void CombinedDialog1ClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
    {
        if (Equals(eventArgs.Parameter, "1") && CombinedCalendar1.SelectedDate is DateTime selectedDate)
        {
            to = selectedDate.AddSeconds(CombinedClock1.Time.TimeOfDay.TotalSeconds);
            lblTo.Text = to.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }


}
