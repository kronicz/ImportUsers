using SuperOffice.CRM.Administration;
using SuperOffice.CRM.Data;
using SuperOffice.Data;
using SuperOffice.Data.SQL;
using SuperOffice.License;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportUsers.Helpers
{
    public class Importer
    {
        private Importer()
        {

        }

        private static Importer obj = null;
        private static readonly object mylockobject = new object();
        public static Importer getInstance()
        {
            lock (mylockobject)
            {
                if (obj == null)
                {
                    obj = new Importer();
                }

            }

            return obj;
        }

        public static Dictionary<string, int> FindUsers()
        {
            AssociateTableInfo tbi = TablesInfo.GetAssociateTableInfo();
            Select select = S.NewSelect();
            select.ReturnFields.Add(tbi.AssociateId);
            select.ReturnFields.Add(tbi.Name);
            select.Restriction = tbi.PersonId.GreaterThan(S.Parameter(0));
            Dictionary<string, int> users = new Dictionary<string, int>();
            using (QueryExecutionHelper qeh = new QueryExecutionHelper(select))
            {
                while(qeh.Reader.Read())
                {
                    users.Add(qeh.Reader.GetString(1), qeh.Reader.GetInt32(0));
                }
            }

            return users;
        }

        public static string GetLicense(int associateId,List<string> options)
        {
            SoUser soUser = SoUser.ManageUser(associateId);
            List<AssociateModuleLicenseLink> license = soUser.GetModuleLicenses().ToList();
            string result = "";
            foreach (var item in license)
            {
                if(options.Contains(item.License.ModuleName))
                    result += item.License.ModuleDescription ;
            }
            return result;
        }

        public static DataTable ConvertToDataTable<T>(IList<T> data)
        {
            PropertyDescriptorCollection properties =
               TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                table.Rows.Add(row);
            }
            return table;

        }
    }
}

