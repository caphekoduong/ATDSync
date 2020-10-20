using ATDSync.Models;
using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace ATDSync
{
    public class SyncService
    {
        public string _fromDB { get; set; }
        public string _toDB { get; set; }
        public SyncService(string fromDB, string toDB)
        {
            _fromDB = fromDB;
            _toDB = toDB;
        }

        public string getNpgDbName()
        {
            using (var connection = new NpgsqlConnection(_fromDB))
            {
                try
                {
                    connection.Open();
                    return connection.Database;
                }
                catch (Exception e)
                {
                    return "Connect failed.";
                }
            }
        }
        public string getSqlDbName()
        {
            using (var sHRMcnn = new SqlConnection(_toDB))
            {
                try
                {
                    sHRMcnn.Open();
                    return sHRMcnn.Database;
                }
                catch (Exception e)
                {
                    return "Connect failed.";
                }
            }
        }

        public List<Attendance> GetAttendances(int sync)
        {
            List<Attendance> atts;
            using (var connection = new NpgsqlConnection(_fromDB))
            {
                connection.Open();

                string query;

                if (sync == 1)
                {
                    query =
                    @"select a.id, b.x_employee_code as manv, a.check_in + interval '7 hours' as tgquet, a.x_sync as isSync
                    from hr_attendance a
                    join hr_employee b on a.employee_id = b.id 
                    where a.check_in is not null
                    and a.x_sync = true

                    union all

                    select a.id, b.x_employee_code, a.check_out + interval '7 hours', a.x_sync2
                    from hr_attendance a
                    join hr_employee b on a.employee_id = b.id 
                    where a.check_out is not null
                    and a.x_sync2 = true

                    order by id
                    ";
                }
                else
                {
                    query =
                    @"select a.id, b.x_employee_code as manv, a.check_in + interval '7 hours' as tgquet, a.x_sync as isSync
                    from hr_attendance a
                    join hr_employee b on a.employee_id = b.id 
                    where a.check_in is not null
                    and (a.x_sync is null or a.x_sync = false)

                    union all

                    select a.id, b.x_employee_code, a.check_out + interval '7 hours', a.x_sync2 
                    from hr_attendance a
                    join hr_employee b on a.employee_id = b.id 
                    where a.check_out is not null
                    and (a.x_sync2 is null or a.x_sync2 = false)

                    order by id
                    ";
                }

                atts = connection.Query<Attendance>(query).ToList();

                connection.Close();
            }
            return atts;
        }

        public void SyncCheckIn()
        {
            List<Attendance> atts;
            using (var connection = new NpgsqlConnection(_fromDB))
            {
                connection.Open();

                string query =
                @"
                select a.id, b.x_employee_code as manv, a.check_in + interval '7 hours' as tgquet, a.x_sync
                from hr_attendance a
                join hr_employee b on a.employee_id = b.id 
                where a.check_in is not null
                and (a.x_sync is null or a.x_sync = false)
                ";

                atts = connection.Query<Attendance>(query).ToList();

                if (atts.Count > 0)
                {
                    using (var sHRMcnn = new SqlConnection(_toDB))
                    {
                        foreach (var att in atts)
                        {
                            var maChamCong = sHRMcnn.Query<string>("SELECT maChamCong FROM tbNhanVien WHERE maNV = @maNV", new { maNV = att.MaNV }).FirstOrDefault();
                            var soMay = sHRMcnn.Query<int>("SELECT soMay FROM tbMayChamCong WHERE tenMay = @tenMay", new { tenMay = "Odoo" }).FirstOrDefault();

                            if (maChamCong != null)
                            {
                                //Console.WriteLine(maChamCong);
                                int macc = int.Parse(maChamCong);

                                var result = sHRMcnn.Query<dynamic>("p_push_fingerPrint", new { ma_cc = macc, somay = soMay, time = att.tgQuet, timeClient = DateTime.Now }, commandType: System.Data.CommandType.StoredProcedure).ToList();

                                if (result.Count > 0)
                                {
                                    string updateString = "UPDATE hr_attendance SET x_sync = true WHERE id = @id";

                                    connection.Execute(updateString, new { id = att.id });
                                }
                            }
                        }
                    }
                }
                connection.Close();
            }
        }
        public void SyncCheckOut()
        {
            List<Attendance> atts;
            using (var connection = new NpgsqlConnection(_fromDB))
            {
                connection.Open();

                string query =
                @"
                select a.id, b.x_employee_code as manv, a.check_out + interval '7 hours' as tgquet, a.x_sync2 as isSync
                from hr_attendance a
                join hr_employee b on a.employee_id = b.id 
                where a.check_out is not null
                and (a.x_sync2 is null or a.x_sync2 = false)
                ";

                atts = connection.Query<Attendance>(query).ToList();

                if (atts.Count > 0)
                {
                    using (var sHRMcnn = new SqlConnection(_toDB))
                    {
                        foreach (var att in atts)
                        {
                            var maChamCong = sHRMcnn.Query<string>("SELECT maChamCong FROM tbNhanVien WHERE maNV = @maNV", new { maNV = att.MaNV }).FirstOrDefault();
                            var soMay = sHRMcnn.Query<int>("SELECT soMay FROM tbMayChamCong WHERE tenMay = @tenMay", new { tenMay = "Odoo" }).FirstOrDefault();

                            if (maChamCong != null)
                            {
                                //Console.WriteLine(maChamCong);
                                int macc = int.Parse(maChamCong);

                                var result = sHRMcnn.Query<dynamic>("p_push_fingerPrint", new { ma_cc = macc, somay = soMay, time = att.tgQuet, timeClient = DateTime.Now }, commandType: System.Data.CommandType.StoredProcedure).ToList();

                                if (result.Count > 0)
                                {
                                    string updateString = "UPDATE hr_attendance SET x_sync2 = true WHERE id = @id";

                                    connection.Execute(updateString, new { id = att.id });
                                }
                            }
                        }
                    }
                }
                connection.Close();
            }
        }

        public void SyncAttendances()
        {
            SyncCheckIn();
            SyncCheckOut();
        }
    }
}
