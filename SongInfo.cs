using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PlayLogger
{
    public class SongInfo
    {
        [Browsable(true)]
        [HideFromDG]
        public long RecordId { get; set; }

        [Browsable(false)]
        public int Id { get; set; }

        [Browsable(true)]
        public string Title { get; set; }

        [Browsable(true)]
        public DateTime PlayTime { get; set; }

        [Browsable(true)]
        public string PlayLocation { get; set; }

        private Dictionary<string, string> m_Fields = null;

        [Browsable(false)]
        public Dictionary<string, string> Fields
        {
            get
            {
                if (m_Fields == null)
                {
                    m_Fields = new Dictionary<string, string>();
                    foreach (var field in DbHandler.SongFields)
                    {
                        m_Fields.Add(field, null);
                    }

                }

                return m_Fields;
            }
        }

        
        public static SongInfo ReadFromXml(string path)
        {
            try
            {
                SongInfo songInfo = new SongInfo();
                XDocument xml = XDocument.Load(path);

                var xSong = xml.Descendants("Song");

                songInfo.PlayTime = System.IO.File.GetLastWriteTime(path);
                songInfo.Title = xSong.Elements("Title").First().Value;

                var enabledFields = new HashSet<string>(DbHandler.SongFields);
                enabledFields.Add("ID");


                var fields = xSong.Elements("Field").Where(x => enabledFields.Contains(x.Attribute("Name").Value));
                foreach (var field in fields)
                {
                    string filedName = field.Attribute("Name").Value;
                    if (filedName == "ID")
                    {
                        songInfo.Id = Convert.ToInt32(field.Value);
                    }
                    else
                    {
                        songInfo.Fields[filedName] = field.Value;
                    }
                }

                return songInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override string ToString()
        {

            string type = null;
            if (Fields.ContainsKey("Type"))
            {
                type = Fields["Type"];
            }

            return string.Format("{0}, {1}, {2}, {3}", Id, Title, PlayLocation, type);
        }
    }
}
