using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImportUsers.Helpers
{
    /// <summary>
    /// Information about one to-be-imported user
    /// </summary>
    public class ImportUserInfo
    {
        public string UID { get; set; }
        public string PWD { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Role { get; set; }
        public string Group { get; set; }
        public int AssociateId { get; set; }
        //public string License { get; set; }
    }

    /// <summary>
    /// File parser for tab-separated file; strict naming convention for required columns, but tolerates any order and any additional columns
    /// </summary>
    public class FileParser
    {
        int[] _fieldMapping;
        List<ImportUserInfo> _userInfos;

        public List<ImportUserInfo> UserInfos { get { return _userInfos; } }

        /// <summary>
        /// Constructor-does-all: Read file, look at header to identify columns, parse the rest
        /// </summary>
        /// <param name="fileName"></param>
        public FileParser(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName, Encoding.GetEncoding(1252));
            CreateMappings(lines[0]);

            _userInfos = new List<ImportUserInfo>(lines.Length - 1);
            for (int lineNo = 1; lineNo < lines.Length; ++lineNo)
            {
                if(!string.IsNullOrWhiteSpace(lines[lineNo]))
                _userInfos.Add(ParseLine(lines[lineNo]));
            }
        }

        /// <summary>
        /// Map required column heading to column number
        /// </summary>
        /// <param name="heading"></param>
        private void CreateMappings(string heading)
        {
            string[] columns = heading.Split('\t');
            _fieldMapping = new int[9];

            _fieldMapping[0] = FindColumn(columns, "username");   //SO Username
            _fieldMapping[1] = FindColumn(columns, "password"); //SO Password
            _fieldMapping[2] = FindColumn(columns, "fullname"); //SO User Tooltip
            _fieldMapping[3] = FindColumn(columns, "firstname");//SO Person First Name
            _fieldMapping[4] = FindColumn(columns, "lastname"); //SO Person Last Name
            _fieldMapping[5] = FindColumn(columns, "email");    //SO Person Email
            _fieldMapping[6] = FindColumn(columns, "company");  //SO Company Name
            _fieldMapping[7] = FindColumn(columns, "role");     //SO User Role
            _fieldMapping[8] = FindColumn(columns, "usergroup");//SO User Usergroup
            //_fieldMapping[9] = FindColumn(columns, "license"); //License

        }

        /// <summary>
        /// Case-insensitive find to identify column headings
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        private int FindColumn(string[] columns, string heading)
        {
            for (int colNo = 0; colNo < columns.Length; ++colNo)
                if (StringComparer.InvariantCultureIgnoreCase.Equals(columns[colNo], heading.Trim()))
                    return colNo;

            throw new Exception("Column " + heading + " not found in heading; sorry I'm not very tolerant about that");
        }

        /// <summary>
        /// Split tab-separated line, and assign via column mapping
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private ImportUserInfo ParseLine(string line)
        {
            string[] columns = line.Split('\t');

            return new ImportUserInfo
            {
                UID = columns[_fieldMapping[0]],
                PWD = columns[_fieldMapping[1]],
                FullName = columns[_fieldMapping[2]],
                FirstName = columns[_fieldMapping[3]],
                LastName = columns[_fieldMapping[4]],
                Email = columns[_fieldMapping[5]],
                Company = columns[_fieldMapping[6]],
                Role = columns[_fieldMapping[7]],
                Group = columns[_fieldMapping[8]],
                //AssociateId = -1
                // License = columns[_fieldMapping[9]]
            };
        }
    }
}
