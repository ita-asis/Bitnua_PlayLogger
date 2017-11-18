using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PlayLogger
{
    public static class DbHandler
    {


        public static List<String> SongFields
        {
            get
            {
                string csvFields = ConfigurationManager.AppSettings["SongFieldsToSave"];

                return new List<string>(csvFields.Split(','));
            }
        }

        private static string extraFiledsCSV()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string field in SongFields)
            {
                sb.AppendFormat(",`{0}`", field);
            }

            return sb.ToString();
        }


        public static void test()
        {
            try
            {
                //var songToInsert = new List<SongInfo>();

                //for (int i = 0; i < 10; i++)
                //{
                //    var song = new SongInfo() { Id = 10 + i, Title = string.Format("new Song {0}", i), PlayTime = DateTime.Now.AddDays(-1 * i) };
                //    song.Fields.Add(string.Format("Field{0}", i), i.ToString());
                //    songToInsert.Add(song);
                //}

                //LogSongInfo(songToInsert);


                SongInfo s = SongInfo.ReadFromXml("C:\\Users\\iasis\\Desktop\\Dads\\vmmplay\\playing.20171101-134651-346.xml");



                var songs = GetHistoryFromDb();
                foreach (var song in songs)
                {
                    song.ReadExtraFieldsFromDb();
                    if (true)
                    {

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static List<SongInfo> GetHistoryFromDb()
        {
            List<SongInfo> history = null;
            var dbCon = DBConnection.Instance();
            if (dbCon.IsConnect())
            {
                history = new List<SongInfo>();
                string query = string.Format("SELECT RecordId,Id,Title,LastPlayTime,PlayLocation FROM playhistory", extraFiledsCSV());
                var cmd = new MySqlCommand(query, dbCon.Connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SongInfo song = new SongInfo()
                        {
                            RecordId = reader.GetInt64(0),
                            Id = reader.GetInt32(1),
                            Title = reader.SafeGetString(2),
                            PlayTime = reader.GetDateTime(3),
                            PlayLocation = reader.SafeGetString(4)
                        };

                        history.Add(song);
                    }
                }

                foreach (var item in history)
                {
                    item.ReadExtraFieldsFromDb();
                }
            }

            return history;
        }

        public static void ReadExtraFieldsFromDb(this SongInfo song)
        {
            var dbCon = DBConnection.Instance();
            if (dbCon.IsConnect())
            {
                string query = string.Format("SELECT FieldName,FieldValue FROM FieldData where RecordId = {0}", song.RecordId);
                var cmd = new MySqlCommand(query, dbCon.Connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string fieldName = reader.SafeGetString(0);
                        string value = reader.SafeGetString(1);
                        if (SongFields.Contains(fieldName))
                        {
                            song.Fields[fieldName] = value;
                        }
                    }
                }

            }
        }

        public static void LogSongInfo(PlayHistorySettings args)
        {
            try
            {

                var dbCon = DBConnection.Instance();
                if (dbCon.IsConnect())
                {
                    var cmdSong = new MySqlCommand("", dbCon.Connection);
                    cmdSong.CommandText = "INSERT INTO playhistory (Id,Title,LastPlayTime,PlayLocation) VALUES (?id,?title,?lastPlay,?playLoc); select last_insert_id();";

                    var cmdFields = new MySqlCommand("", dbCon.Connection);
                    cmdFields.CommandText = "INSERT INTO FieldData (RecordId,FieldName,FieldValue) VALUES (?recordId,?fName,?fValue)";
                    cmdFields.Prepare();

                    IEnumerable<SongInfo> songs = readSongInfo(args);
                    foreach (var song in songs)
                    {

                        if (!isSongInfoExistsInDb(song, args.PlayLocation))
                        {
                            cmdSong.Parameters.Clear();
                            cmdSong.Parameters.AddWithValue("?id", song.Id);
                            cmdSong.Parameters.AddWithValue("?title", song.Title);
                            cmdSong.Parameters.AddWithValue("?lastPlay", song.PlayTime);
                            cmdSong.Parameters.AddWithValue("?playLoc", args.PlayLocation);

                            song.RecordId = Convert.ToInt64(cmdSong.ExecuteScalar());

                            foreach (var item in song.Fields)
                            {
                                cmdFields.Parameters.Clear();
                                cmdFields.Parameters.AddWithValue("?recordId", song.RecordId);
                                cmdFields.Parameters.AddWithValue("?fName", item.Key);
                                cmdFields.Parameters.AddWithValue("?fValue", item.Value);
                                cmdFields.ExecuteNonQuery();
                            }

                        }

                    }
                }

            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }

        private static bool isSongInfoExistsInDb(SongInfo song, string i_PlayLocation)
        {
            var dbCon = DBConnection.Instance();
            if (dbCon.IsConnect())
            {
                var cmd = new MySqlCommand("", dbCon.Connection);
                cmd.CommandText = "Select RecordId from playhistory Where Id = ?id AND LastPlayTime = ?lastPlay AND PlayLocation = ?playLoc;";

                cmd.Parameters.AddWithValue("?id", song.Id);
                cmd.Parameters.AddWithValue("?lastPlay", song.PlayTime);
                cmd.Parameters.AddWithValue("?playLoc", i_PlayLocation);

                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read();
                }
            }

            return false;
        }

        private static IEnumerable<SongInfo> readSongInfo(PlayHistorySettings args)
        {
            List<SongInfo> res = null;
            try
            {

                IEnumerable<string> files = null;
                if (args.ReadFileListOnly)
                {
                    files = args.Files;
                }
                else
                {
                    if (System.IO.Directory.Exists(args.LastPlayedXmlDir))
                    {
                        files = System.IO.Directory.EnumerateFiles(args.LastPlayedXmlDir, "*.xml");
                    }
                }

                res = new List<SongInfo>();
                if (files != null)
                {
                    foreach (var filePath in files)
                    {
                        var song = SongInfo.ReadFromXml(filePath);
                        if (song != null)
                        {
                            res.Add(song);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
            return res;

        }

        public static string SafeGetString(this MySqlDataReader reader, int colIndex)
        {
            if (!reader.IsDBNull(colIndex))
                return reader.GetString(colIndex);
            return string.Empty;
        }

        public static void RemoveSongsFromDb(IEnumerable<SongInfo> i_SongsToRemove)
        {
            try
            {
                var dbCon = DBConnection.Instance();
                if (dbCon.IsConnect() && i_SongsToRemove != null && i_SongsToRemove.Any())
                {
                    string ids = string.Join(",", from song in i_SongsToRemove select song.RecordId.ToString());
                    var cmdDel = new MySqlCommand("", dbCon.Connection);
                    cmdDel.CommandText = string.Format("Delete FROM playhistory WHERE RecordId in ({0}); Delete FROM FieldData WHERE RecordId in ({0});", ids);
                    cmdDel.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MainViewModel.LogException(ex);
            }
        }
    }
}
