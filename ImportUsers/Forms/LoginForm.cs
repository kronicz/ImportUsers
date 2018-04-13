using ImportUsers.Forms;
using SuperOffice;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Xml;

namespace ImportUsers
{
    public partial class LoginForm : Form
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public LoginForm()
        {
            InitializeComponent();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var superoffice_db = ConfigurationManager.GetSection("SuperOffice/Data/Database") as NameValueCollection;
            var server = superoffice_db.Get("Server");
            var db = superoffice_db.Get("Database");
            var tblPrefix = superoffice_db.Get("TablePrefix");
            txtDatabase.Text = db;
            txtPrefix.Text = tblPrefix;
            txtSqlServer.Text = server;

            var superoffice_user = ConfigurationManager.GetSection("SuperOffice/Data/Explicit") as NameValueCollection;
            var db_user = superoffice_user.Get("DBUser");
            var db_pwd = superoffice_user.Get("DBPassword");
            txtDbUsername.Text = db_user;
            txtDbPwd.Text = db_pwd;

            if (server == "" && db == "" && tblPrefix == "" && db_user == "" && db_pwd == "")
                btnLogin.Enabled = false;
            
        }
        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsername.Text == "" || txtPassword.Text == "")
            {
                MessageBox.Show("Please provide Username and Password");
                return;
            }
            else
            {
                Username = txtUsername.Text;
                Password = txtPassword.Text;
                //Sosession
                try
                {
                    using (var _session = SoSession.Authenticate(Username, Password))
                    {
                        string sessionString = string.Empty;
  
                        //sessionString = _session.Suspend();
                        //MessageBox.Show("Logged In");
                        this.Hide();
                        MainWindow main = new MainWindow(Username, Password);
                        //main.WindowState = FormWindowState.Maximized;
                        main.Show();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + " try again");
                }
            }
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            if (txtSqlServer.Text != "" && txtDatabase.Text != "" && txtPrefix.Text != "" && txtDbUsername.Text != "" && txtDbPwd.Text != "")
            {
                MessageBox.Show("You must restart for changes to take effect");
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                xmlDoc.SelectSingleNode("//SuperOffice/Data/Database/add[@key='Server']").Attributes["value"].Value = txtSqlServer.Text;
                xmlDoc.SelectSingleNode("//SuperOffice/Data/Database/add[@key='Database']").Attributes["value"].Value = txtDatabase.Text;
                xmlDoc.SelectSingleNode("//SuperOffice/Data/Database/add[@key='TablePrefix']").Attributes["value"].Value = txtPrefix.Text;
                xmlDoc.SelectSingleNode("//SuperOffice/Data/Explicit/add[@key='DBUser']").Attributes["value"].Value = txtDbUsername.Text;
                xmlDoc.SelectSingleNode("//SuperOffice/Data/Explicit/add[@key='DBPassword']").Attributes["value"].Value = txtDbPwd.Text;

                xmlDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);

                ConfigurationManager.RefreshSection("SuperOffice/Data/Database");
                ConfigurationManager.RefreshSection("SuperOffice/Data/Explicit");
                btnLogin.Enabled = false;
            }
            else
                MessageBox.Show("Error: Please input values in Database Connection");
        }
    }
}
