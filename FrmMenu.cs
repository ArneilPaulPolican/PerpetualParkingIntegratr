using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace PerpetualIntegrator
{
    public partial class FrmMenu : Form
    {
        public static Data.PosDatabaseDataContext db;
        public bool onProcess = false;
        public static bool isProccessing;
        delegate void AppendTextDelegate(string text);

        public FrmMenu()
        {
            InitializeComponent();
            db = new Data.PosDatabaseDataContext(SysGlobal.ConnectionStringConfig());
            InitializeObjectValue();
        }

        public void InitializeObjectValue()
        {
            DisableObject();
            db = new Data.PosDatabaseDataContext(SysGlobal.ConnectionStringConfig());

            string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SysCurrent.json");
            String json;
            using (StreamReader trmRead = new StreamReader(settingsPath))
            {
                json = trmRead.ReadToEnd();
            }
            JavaScriptSerializer js = new JavaScriptSerializer();
            Models.SysCurrent s = js.Deserialize<Models.SysCurrent>(json);

            txtPOSConnString.Text = s.POSConnectionString;
            txtParkingConnString.Text = s.ParkingConnectionString;
            txtLastExportIdNumber.Text = Convert.ToString(s.LastExportChargeIdNumber);
            //txtUseLastExportIdNumber.Checked = s.UseLastExportChargeIdNumber;
            txtItemCode.Text = s.DefaultItemCode;
            txtCustomerCode.Text = s.DefaultCustomerCode;
        }

        public void Fetch(string parkingConnectionString, string defaultCustomerCode, string defaultItemCode, long lastExportChargeIdNumber, bool useLastExportChargeIdNumber)
        {
            try
            {
                if (isProccessing == true)
                {

                    string DocumentRef = "";
                    string ParkingId = "";
                    DateTimeOffset dto = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

                    dto = new DateTimeOffset(2019, 08, 09, 0, 0, 0, TimeSpan.Zero);
                    Console.WriteLine("{0} --> Unix Seconds: {1}", dto, dto.ToUnixTimeSeconds());

                    string Query2 = "";
                    string conString = parkingConnectionString;


                    Query2 = "SELECT export_charge.total_money, export_charge.outtime, car_card.card_no, export_charge.id " +
                                "FROM Export_charge INNER JOIN Car_card ON Export_charge.card_id = Car_card.id " +
                                "WHERE(((export_charge.id) > " + lastExportChargeIdNumber + "))";

                    // Default Item
                    var DefaultItem = from d in db.MstItems where d.ItemCode == defaultItemCode select d;
                    if (DefaultItem.Any())
                    {
                        var item = DefaultItem.FirstOrDefault();
                        var DefaultCustomer = from d in db.MstCustomers where d.CustomerCode == defaultCustomerCode select d;
                        if (DefaultCustomer.Any())
                        {
                            var customer = DefaultCustomer.FirstOrDefault();
                            //MySqlConnection MyConn2 = null;
                            //MySqlCommand myCommand = null;
                            //MySqlDataReader myReader = null;
                            try
                            {
                                using (OdbcConnection con = new OdbcConnection(conString))
                                {
                                    OdbcCommand cmd = new OdbcCommand(Query2, con);
                                    OdbcDataAdapter da = new OdbcDataAdapter(cmd);
                                    try
                                    {
                                        DataSet ds = new DataSet();
                                        da.Fill(ds);
                                        for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                                        {
                                            string strDate = DateTime.Now.ToString("MM/dd/yyyy");
                                            //string DocumentRef = myReader.GetInt64(27).ToString();
                                            // Date Time
                                            //double timestamp = Convert.ToDouble(myReader.GetDecimal(15));
                                            double timestamp = Convert.ToDouble(ds.Tables[0].Rows[i]["outtime"].ToString());
                                            System.DateTime dateTime = new System.DateTime(1970, 1, 1);
                                            dto = new DateTimeOffset(Convert.ToInt32(dateTime.ToString("yyyy")), Convert.ToInt32(dateTime.ToString("MM")), Convert.ToInt32(dateTime.ToString("dd")), Convert.ToInt32(dateTime.ToString("hh")), Convert.ToInt32(dateTime.ToString("mm")), Convert.ToInt32(dateTime.ToString("ss")), TimeSpan.Zero);

                                            dateTime = dateTime.AddSeconds(timestamp);
                                            strDate = dateTime.ToString("MM/dd/yyyy");
                                            decimal dblAmount = Convert.ToDecimal(ds.Tables[0].Rows[i]["total_money"].ToString());
                                            ParkingId = ds.Tables[0].Rows[i]["id"].ToString();
                                            DocumentRef = ds.Tables[0].Rows[i]["card_no"].ToString();

                                            //Console.WriteLine(ds.Tables[0].Rows[i]["total_money"].ToString());
                                            //Console.WriteLine(ds.Tables[0].Rows[i]["outtime"].ToString());
                                            //Console.WriteLine(ds.Tables[0].Rows[i]["card_no"].ToString());
                                            //Console.WriteLine(ds.Tables[0].Rows[i]["id"].ToString());

                                            Console.WriteLine("Total Money: " + dblAmount + " - OutTime" + strDate + "; Card No" + DocumentRef);
                                            Console.WriteLine(DocumentRef + " - " + dblAmount + " - " + strDate);
                                            var SalesDraft = from d in db.TrnSalesDrafts where d.ParkingId == Convert.ToString(ParkingId) select d;
                                            if (SalesDraft.Any())
                                            {
                                                Console.WriteLine("This Existed");
                                            }
                                            else
                                            {
                                                if (isProccessing == true)
                                                {
                                                    GetActivityTxt(DocumentRef + " - " + dblAmount + " - " + strDate + " - " + strDate + " - " + ParkingId);

                                                    Data.TrnSalesDraft newSales = new Data.TrnSalesDraft()
                                                    {
                                                        DocRef = DocumentRef,
                                                        DocDate = Convert.ToDateTime(strDate),
                                                        ItemCode = item.ItemCode,
                                                        ItemId = item.Id,
                                                        Price = dblAmount,
                                                        Quantity = 1,
                                                        Amount = dblAmount,
                                                        CustomerCode = customer.CustomerCode,
                                                        Customer = customer.Customer,
                                                        ContactPerson = customer.ContactPerson,
                                                        Address = customer.Address,
                                                        PhoneNumber = customer.ContactNumber,
                                                        MobilePhoneNumber = customer.ContactNumber,
                                                        ParkingId = ParkingId
                                                    };
                                                    db.TrnSalesDrafts.InsertOnSubmit(newSales);
                                                    db.SubmitChanges();

                                                    SaveLastExportIdNumber(ParkingId);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                        GetActivityTxt("Trying to connect with ODBC");
                                    }
                                    con.Close();
                                }
                                //MyConn2 = new MySqlConnection(parkingConnectionString);
                                //myCommand = new MySqlCommand(Query2, MyConn2);
                                //MyConn2.Open();
                                //myCommand.CommandTimeout = 10;
                                //myReader = myCommand.ExecuteReader();
                                //while (myReader.Read())
                                //{
                                //}
                            }
                            catch (MySqlException ex)
                            {
                                GetActivityTxt("Error: {0}" + ex.ToString());
                                Console.WriteLine(ex.ToString());
                            }
                            finally
                            {

                            }
                        }
                    }
                    InitializeObject();
                    isProccessing = false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                System.Diagnostics.Debug.WriteLine(e);
                GetActivityTxt("Error on trying to fetch Data from ODBC");
                isProccessing = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>

        public void GetActivityTxt(string Activity)
        {
            if (txtActivity.InvokeRequired)
            {
                txtActivity.Invoke(new AppendTextDelegate(this.GetActivityTxt), new object[] { Activity });
            }
            else
            {
                txtActivity.Text += Activity + " \r\n\n";
                txtActivity.SelectionStart = txtActivity.Text.Length;
                txtActivity.ScrollToCaret();
            }
        }

        public void SaveLastExportIdNumber(String ParkingId)
        {
            try
            {
                Models.SysCurrent settingsData = new Models.SysCurrent()
                {
                    LastExportChargeIdNumber = Convert.ToInt64(ParkingId)
                };

                String json = new JavaScriptSerializer().Serialize(settingsData);

                String settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SysCurrent.json");
                File.WriteAllText(settingsPath, json);

            }
            catch (Exception e)
            {
                GetActivityTxt("Error on Saving Last Parking Id");
            }
        }

        public void InitializeObject()
        {
            try
            {
                //DisableObject();
                //db = new Data.PosDatabaseDataContext(SysGlobal.ConnectionStringConfig());

                string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SysCurrent.json");
                String json;
                using (StreamReader trmRead = new StreamReader(settingsPath))
                {
                    json = trmRead.ReadToEnd();
                }
                JavaScriptSerializer js = new JavaScriptSerializer();
                Models.SysCurrent s = js.Deserialize<Models.SysCurrent>(json);

                ((Control)this.tabPage2).Enabled = true;
                SetToUI(Convert.ToString(s.LastExportChargeIdNumber));
            }
            catch (Exception e)
            {
                GetActivityTxt(e.Message);
            }
        }

        public void SetToUI(string v)
        {
            if (txtLastExportIdNumber.InvokeRequired)
            {
                txtLastExportIdNumber.Invoke(new AppendTextDelegate(this.SetToUI), new object[] { Convert.ToString(v) });
            }
            else
            {
                txtLastExportIdNumber.Text = Convert.ToString(v);
                //txtLastExportIdNumber.SelectionStart = txtActivity.Text.Length;
                //txtActivity.ScrollToCaret();
            }
        }

        public void EnableObject()
        {
            txtPOSConnString.Enabled = true;
            txtParkingConnString.Enabled = true;
            txtLastExportIdNumber.Enabled = true;
            //txtUseLastExportIdNumber.Enabled = true;
            txtCustomerCode.Enabled = true;
            txtItemCode.Enabled = true;
        }
        public void DisableObject()
        {
            txtPOSConnString.Enabled = false;
            txtParkingConnString.Enabled = false;
            txtLastExportIdNumber.Enabled = false;
            //txtUseLastExportIdNumber.Enabled = false;
            txtCustomerCode.Enabled = false;
            txtItemCode.Enabled = false;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            isProccessing = true;
            GetActivityTxt("Integrator Start");
            ((Control)this.tabPage2).Enabled = false;
            time();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            isProccessing = false;
            GetActivityTxt("Integrator Stop");
            ((Control)this.tabPage2).Enabled = true;
            timer.Dispose();
        }
        private void SaveSettingsValue()
        {
            try
            {
                Models.SysCurrent settingsData = new Models.SysCurrent()
                {
                    POSConnectionString = txtPOSConnString.Text,
                    ParkingConnectionString = txtParkingConnString.Text,
                    LastExportChargeIdNumber = Convert.ToInt64(txtLastExportIdNumber.Text),
                    //UseLastExportChargeIdNumber = txtUseLastExportIdNumber.Checked,
                    DefaultCustomerCode = txtCustomerCode.Text,
                    DefaultItemCode = txtItemCode.Text
                };

                String json = new JavaScriptSerializer().Serialize(settingsData);

                String settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SysCurrent.json");
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void BtnSave_Click_1(object sender, EventArgs e)
        {
            SaveSettingsValue();
            DisableObject();
            btnEdit.Enabled = true;
            btnSave.Enabled = false;
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            EnableObject();
            btnSave.Enabled = true;
            btnEdit.Enabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        private static System.Threading.Timer timer;

        public void time()
        {
            var timerState = new TimerState { Counter = 0 };

            timer = new System.Threading.Timer(
                callback: new TimerCallback(TimerTask),
                state: timerState,
                dueTime: 1000,
                period: 2000);
        }

        private void TimerTask(object timerState)
        {
            if (onProcess == false)
            {
                onProcess = true;
                Console.WriteLine("timer enable");
                string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SysCurrent.json");
                String json;
                using (StreamReader trmRead = new StreamReader(settingsPath))
                {
                    json = trmRead.ReadToEnd();
                }
                JavaScriptSerializer js = new JavaScriptSerializer();
                Models.SysCurrent s = js.Deserialize<Models.SysCurrent>(json);

                Fetch(s.ParkingConnectionString, s.DefaultCustomerCode, s.DefaultItemCode, s.LastExportChargeIdNumber, s.UseLastExportChargeIdNumber);
                onProcess = false;
            }
        }

        class TimerState
        {
            public int Counter;
        }
    }
}