using ImportUsers.Helpers;
using SuperOffice;
using SuperOffice.CRM.Administration;
using SuperOffice.CRM.Cache;
using SuperOffice.CRM.Data;
using SuperOffice.CRM.Rows;
using SuperOffice.CRM.Services;
using SuperOffice.Data;
using SuperOffice.Data.SQL;
using SuperOffice.License;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImportUsers.Forms
{
    public partial class MainWindow : Form
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool DefaultConfig { get; set; }
        public string DefaultRole { get; set; }
        public string DefaultGroup { get; set; }
        public string DefaultLicense { get; set; }
        public List<string> LookupList { get; set; }
        FileParser p;
        public Dictionary<string, int> _contacts = new Dictionary<string, int>();
        public Dictionary<string, int> _roles = new Dictionary<string, int>();
        public Dictionary<string, int> _groups = new Dictionary<string, int>();
        SelectableMDOListItem[] roles;
        UserGroup[] groups;
        CheckBox headerCheckbox = new CheckBox();
        DataTable dtUsers;
        int selectedBox = 0;

        public MainWindow(string username,string password)
        {
            Username = username;
            Password = password;
            DefaultConfig = false;
            InitializeComponent();
            
            UserAgent _agent = new UserAgent();
            roles = _agent.GetAllRoles(SuperOffice.Data.RoleType.Employee);
            for(int i = 0; i < roles.Length;i++)
                _roles.Add(roles[i].Name, roles[i].Id);

            groups = _agent.GetAllUserGroups(false);
            for (int j = 0; j < groups.Length; j++)
                _groups.Add(groups[j].Value, groups[j].Id);

            cmbRole.DataSource = roles.ToList();
            cmbRole.DisplayMember = "Name";
            cmbUserGroup.DataSource = groups.ToList();
            cmbUserGroup.DisplayMember = "Value";

            LicenseAgent agent = new LicenseAgent();

            ExtendedLicenseInfo extendedLicense = agent.GetLicenseFromDB("SuperOffice");
            ExtendedModuleLicense[] moduleLicense = extendedLicense.ExtendedModuleLicenses;
            ExtendedModuleLicense sales_users;
            ExtendedModuleLicense service_users;
            ExtendedModuleLicense complete_users;
            sales_users = moduleLicense.FirstOrDefault(c => c.Current.ModuleName.Equals(SoLicenseNames.SuperLicenseSalesPro.Substring(SoLicenseNames.SuperLicenseSalesPro.LastIndexOf('.') + 1)));
            service_users = moduleLicense.FirstOrDefault(c => c.Current.ModuleName.Equals(SoLicenseNames.SuperLicenseServicePro.Substring(SoLicenseNames.SuperLicenseServicePro.LastIndexOf('.') + 1)));
            complete_users = moduleLicense.FirstOrDefault(c => c.Current.ModuleName.Equals(SoLicenseNames.SuperLicenseComplete.Substring(SoLicenseNames.SuperLicenseComplete.LastIndexOf('.') + 1)));
            Dictionary<string,string> lic = new Dictionary<string, string>();
            LookupList = new List<string>();

            if (sales_users != null)
            {
                lic.Add(sales_users.Current.ModuleDescription + " (" + sales_users.NumberOfLicensesTotal + ")", sales_users.Current.ModuleName);
                LookupList.Add(sales_users.Current.ModuleName);
            }
            if (service_users != null)
            {
                lic.Add(service_users.Current.ModuleDescription + " (" + service_users.NumberOfLicensesTotal + ")", service_users.Current.ModuleName);
                LookupList.Add(service_users.Current.ModuleName);
            }
            if (complete_users != null)
            {
                lic.Add(complete_users.Current.ModuleDescription + " (" + complete_users.NumberOfLicensesTotal + ")", complete_users.Current.ModuleName);
                LookupList.Add(complete_users.Current.ModuleName);
            }
            cmbLicense.DisplayMember = "Key";
            cmbLicense.DataSource = lic.ToList();

        }

        private void btnLoadFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var _session = SoSession.Authenticate(Username, Password))
                {
                    DataGridViewComboBoxColumn rolesColumn = (DataGridViewComboBoxColumn)dtUsersList.Columns["Role"];
                    rolesColumn.DataSource = roles.ToList();
                    rolesColumn.DisplayMember = "Name";

                    DataGridViewComboBoxColumn groupsColumn = (DataGridViewComboBoxColumn)dtUsersList.Columns["UserGroup"];
                    groupsColumn.DataSource = groups.ToList();
                    groupsColumn.DisplayMember = "Value";
                    p = new FileParser(openFileDialog.FileName);

                    MessageBox.Show("File parsed, " + p.UserInfos.Count.ToString() + " users read\n");

                    foreach (ImportUserInfo ui in p.UserInfos)
                    {
                        ContactTableInfo cti = TablesInfo.GetContactTableInfo();
                        OwnerContactLinkTableInfo octi = TablesInfo.GetOwnerContactLinkTableInfo();
                        Select findOc = S.NewSelect("Find OC");

                        findOc.JoinRestriction.InnerJoin(cti.ContactId.Equal(octi.ContactId));

                        // if contact name contains a comma, assume that pre-comma is name and post-comma is department (db & file are set up that way)
                        if (ui.Company.Contains(","))
                            findOc.Restriction = cti.Name.Equal(S.Parameter(ui.Company.Split(',')[0].Trim())).
                                And(cti.Department.Equal(S.Parameter(ui.Company.Split(',')[1].Trim())));
                        else
                            findOc.Restriction = cti.Name.Equal(S.Parameter(ui.Company));

                        findOc.ReturnFields.Add(cti.ContactId);

                        int ocId = QueryExecutionHelper.ExecuteTypedScalar<int>(findOc);

                        if (ocId == 0)
                        {
                            //MessageBox.Show("Owner company " + ui.Company + "(referenced by " + ui.UID + ")  does not exist OR is not an Owner Company - setting to License Owner " + SoSystemInfo.GetCurrent().CompanyName);
                            ui.Company = SoSystemInfo.GetCurrent().CompanyName;
                            if (!_contacts.ContainsKey(SoSystemInfo.GetCurrent().CompanyName))
                                _contacts.Add(ui.Company, SoSystemInfo.GetCurrent().CompanyId);
                        }
                        else if (!_contacts.ContainsKey(ui.Company))
                            _contacts.Add(ui.Company, ocId);

                        if (!_roles.ContainsKey(ui.Role))
                            ui.Role = DefaultRole;
                        if (!_groups.ContainsKey(ui.Group))
                            ui.Group = DefaultGroup;
                    }
                    Dictionary<string, int> users = Importer.FindUsers();

                    dtUsersList.AutoGenerateColumns = false;
                    dtUsersList.Columns["FirstName"].DataPropertyName = "FirstName";
                    dtUsersList.Columns["LastName"].DataPropertyName = "LastName";
                    dtUsersList.Columns["FullName"].DataPropertyName = "FullName";
                    dtUsersList.Columns["UserName"].DataPropertyName = "UID";
                    dtUsersList.Columns["Email"].DataPropertyName = "Email";
                    dtUsersList.Columns["Role"].DataPropertyName = "Role";
                    dtUsersList.Columns["UserGroup"].DataPropertyName = "Group";
                    dtUsersList.Columns["Company"].DataPropertyName = "Company";
                    dtUsersList.Columns["AssociateId"].DataPropertyName = "AssociateId";
                    dtUsers = Importer.ConvertToDataTable<ImportUserInfo>(p.UserInfos);
                    dtUsersList.DataSource = dtUsers;
                    //dtUsersList.DataSource = p.UserInfos;
                    progressBar.Value = 0;
                    foreach (DataGridViewRow item in dtUsersList.Rows)
                    {
                        string username = item.Cells["UserName"].Value.ToString();
                        if (users.ContainsKey(username))
                        {
                            item.Cells["Status"].Value = "Exists";
                            item.Cells["AssociateId"].Value = users[username];
                            string license = Importer.GetLicense(users[username], LookupList);
                            item.Cells["AssignedLicenses"].Value = license;
                        }
                        else
                        {
                            item.Cells["AssociateId"].Value = DBNull.Value;
                            item.Cells["Status"].Value = "New";
                            item.Cells["AssignedLicenses"].Value = "";
                        }
                    }

                }
                btnProcess.Enabled = true;
                progressBar.Value = 0;
                txtSearch.Text = "";
                lblSearch.Visible = true;
                txtSearch.Visible = true;
            }

        }
        private void MainWindow_FormClosing(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            if(!DefaultConfig)
            {
                btnLoadFile.Enabled = false;
                btnProcess.Enabled = false;
            }
            Point headerCellLocation = dtUsersList.GetCellDisplayRectangle(0, -1, true).Location;
            headerCheckbox.Location = new Point(headerCellLocation.X + 8, headerCellLocation.Y + 2);
            
            headerCheckbox.Size = new Size(15, 15);
            headerCheckbox.Click += new EventHandler(HeaderCheckbox_Clicked);
            dtUsersList.Controls.Add(headerCheckbox);

            DataGridViewCheckBoxColumn checkboxColumn = new DataGridViewCheckBoxColumn();
            checkboxColumn.HeaderText = "";
            checkboxColumn.Width = 30;
            checkboxColumn.Name = "selectColumn";
            dtUsersList.Columns.Insert(0, checkboxColumn);

            dtUsersList.CellContentClick += new DataGridViewCellEventHandler(DataGridView_CellClick);
            lblSearch.Visible = false;
            txtSearch.Visible = false;
        }

        private void HeaderCheckbox_Clicked(object sender, EventArgs e)
        {
            //Necessary to end the edit mode of the Cell.
            dtUsersList.EndEdit();

            //Loop and check and uncheck all row CheckBoxes based on Header Cell CheckBox.
            foreach (DataGridViewRow row in dtUsersList.Rows)
            {
                DataGridViewCheckBoxCell checkBox = (row.Cells["selectColumn"] as DataGridViewCheckBoxCell);
                checkBox.Value = headerCheckbox.Checked;
                if (headerCheckbox.Checked)
                    selectedBox = dtUsersList.Rows.Count;
                else
                    selectedBox = 0;
            }
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //Check to ensure that the row CheckBox is clicked.
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                //Loop to verify whether all row CheckBoxes are checked or not.
                bool isChecked = true;
                foreach (DataGridViewRow row in dtUsersList.Rows)
                {
                    if (Convert.ToBoolean(row.Cells["selectColumn"].EditedFormattedValue) == false)
                    {
                        isChecked = false;
                        if (selectedBox > 0)
                            selectedBox--;
                        break;
                    }
                }
                headerCheckbox.Checked = isChecked;
                selectedBox ++;
            }
        }

        private void btnDefaults_Click(object sender, EventArgs e)
        {
            DefaultGroup = cmbUserGroup.GetItemText(cmbUserGroup.SelectedItem);
            DefaultRole = cmbRole.GetItemText(cmbRole.SelectedItem);
            DefaultLicense = ((KeyValuePair<string,string>)cmbLicense.SelectedValue).Value.ToString();//cmbLicense.GetItemText(cmbLicense.SelectedItem);
            DefaultConfig = true;
            btnLoadFile.Enabled = true;
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            using (SoSession.Authenticate(Username, Password))
            {        
                progressBar.Value = 0;
                progressBar.Maximum = selectedBox;
                progressBar.Step = 1;
                bool noneSelected = true;
                foreach (DataGridViewRow item in dtUsersList.Rows)
                {
                    if(Convert.ToBoolean(item.Cells["selectColumn"].Value) == true)
                    { 
                        string associateId = item.Cells["AssociateId"].Value.ToString();
                        string firstname = item.Cells["FirstName"].Value.ToString();
                        string lastname = item.Cells["LastName"].Value.ToString();
                        string fullname = item.Cells["FullName"].Value.ToString();
                        string username = item.Cells["UserName"].Value.ToString();
                        string email = item.Cells["Email"].Value.ToString();
                        string role = item.Cells["Role"].Value.ToString();
                        string group = item.Cells["UserGroup"].Value.ToString();
                        string company = item.Cells["Company"].Value.ToString();
                        bool processed = false;
                        if (associateId != "")
                        {
                            processed = updateUser(associateId, firstname, lastname, fullname, username, email, role, group, DefaultLicense, item);
                        }else
                        {
                            processed = createUser(firstname, lastname, fullname, username, email, role, group, company,DefaultLicense, item);
                        }
                        if (processed)
                        {
                            progressBar.PerformStep();
                            item.Cells["Status"].Value = "Processed";
                        }
                        noneSelected = false;
                    }
                }
                if (noneSelected)
                    MessageBox.Show("No entries selected");
            }
        }

        private bool createUser(string firstname,string lastname,string fullname,string username,string email, string role,string group,string company,string license, DataGridViewRow item)
        {
            bool result = false;
            try
            {
                // find person by firstname, lastname & owner contact
                SuperOffice.CRM.Entities.Person.CustomSearch pc = new SuperOffice.CRM.Entities.Person.CustomSearch();
                pc.Restriction = pc.TableInfo.Firstname.Equal(S.Parameter(firstname)).
                    And(pc.TableInfo.Lastname.Equal(S.Parameter(lastname))).
                    And(pc.TableInfo.ContactId.Equal(S.Parameter(_contacts[company])));

                SuperOffice.CRM.Entities.Person p = SuperOffice.CRM.Entities.Person.GetFromCustomSearch(pc);

                // we either found an existing person, or got a blank, ready-to-populate one
                if (p.IsNew)
                {
                    p.SetDefaults(_contacts[company]);

                    p.Firstname = firstname;
                    p.Lastname = lastname;
                }

                // always set userid into number field, for convenience
                p.PersonNumber = username;

                // find existing email, or create a new one
                EmailRow em = null;
                if (p.Emails.Count == 0)
                {
                    em = p.Emails.AddNew();
                    em.SetDefaults();
                }
                else
                    em = p.Emails[0];

                // always set correct email; we have just the one address
                em.EmailAddress = email;
                em.Protocol = "SMTP";

                // save complete person entity
                p.Save();

                // if person is associate - get him/her; otherwise create a new SoUser
                SoUser user;
                if (AssociateCache.GetCurrent().IsPersonAssociate(p.PersonId))
                {
                    user = SoUser.ManageUserFromPersonId(p.PersonId)[0];
                }
                else
                {
                    user = SoUser.CreateNew(p.PersonId, UserType.InternalAssociate);
                }

                // set our various properties
                user.SetPassword(username);
                user.GroupIdx = _groups[group];
                user.OtherGroupIds = new int[0];

                user.RoleIdx = _roles[role];
                user.LogonName = username;
                user.Tooltip = fullname + " (" + company + ")";

                // add licenses
                if (user.GetModuleLicense("SuperOffice", DefaultLicense).CanAssign)
                {
                    user.GetModuleLicense("SuperOffice", DefaultLicense).Assigned = true;
                    item.Cells["AssignedLicenses"].Value = "Assigned Default";
                }
                else
                    item.Cells["AssignedLicenses"].Value = "Cannot Assign";
                //user.GetModuleLicense(SoLicenseNames.SuperLicenseServicePro).Assigned = true;
                /*user.GetModuleLicense(SoLicenseNames.User).Assigned = true;
                user.GetModuleLicense(SoLicenseNames.Web).Assigned = true;*/
                user.GetModuleLicense(SoLicenseNames.VisibleFor).Assigned = true;

                // save the user
                user.Save();
                result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return result;
        }
        private bool updateUser(string associateId,string firstname,string lastname,string fullname, string username, string email, string role, string group,string license, DataGridViewRow item)
        {
            bool result = false;
            try
            {
                SoUser user = SoUser.ManageUser(Convert.ToInt32(associateId));
                SuperOffice.CRM.Entities.Person p = user.Person;
                p.Firstname = firstname;
                p.Lastname = lastname;
                //p.PersonNumber = username;
                String pwd = username;
                EmailRow em = null;
                if (p.Emails.Count == 0)
                {
                    em = p.Emails.AddNew();
                    em.SetDefaults();
                }
                else
                    em = p.Emails[0];

                // always set correct email; we have just the one address
                em.EmailAddress = email;
                em.Protocol = "SMTP";

                // save complete person entity
                p.Save();
                //Console.WriteLine("\tPerson/email done");

                // set our various properties
                user.SetPassword(pwd);
                user.GroupIdx = _groups[group];
                user.OtherGroupIds = new int[0];

                user.RoleIdx = _roles[role];
                user.LogonName = username;
                user.Tooltip = fullname + " (" + SoSystemInfo.GetCurrent().CompanyName + ")";

                // add licenses
                if (user.GetModuleLicense("SuperOffice", DefaultLicense).CanAssign)
                {
                    user.GetModuleLicense("SuperOffice", DefaultLicense).Assigned = true;
                    item.Cells["AssignedLicenses"].Value = "Assigned Default";
                }
                else
                    item.Cells["AssignedLicenses"].Value = "Cannot Assign";
                //user.GetModuleLicense(SoLicenseNames.SuperLicenseServicePro).Assigned = true;
                /*user.GetModuleLicense(SoLicenseNames.User).Assigned = true;
                user.GetModuleLicense(SoLicenseNames.Web).Assigned = true;*/
                user.GetModuleLicense(SoLicenseNames.VisibleFor).Assigned = true;

                // save the user
                user.Save();
                //Console.WriteLine("\tUser saved\n");
                result = true;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            
            return result;
        }
        private void txtSearch_KeyUp(object sender, EventArgs e)
        {
           string search = txtSearch.Text;
            DataTable dt = (DataTable)dtUsersList.DataSource;
            dt.DefaultView.RowFilter = string.Format("FirstName like '%{0}%' or LastName like '%{0}%'", search);
            
            /*
            foreach (DataGridViewRow item in dtUsersList.Rows)
            {
                string firstname = item.Cells["FirstName"].Value.ToString();
                string lastname = item.Cells["LastName"].Value.ToString();
                if (!firstname.Contains(txtSearch.Text) || !lastname.Contains(txtSearch.Text))
                {
                    CurrencyManager currManager = (CurrencyManager)BindingContext[dtUsersList.DataSource];
                    currManager.SuspendBinding();
                    item.Visible = false;
                    currManager.ResumeBinding();
                }
                else
                    item.Visible = true;
                

            }*/
        }
    }
}
