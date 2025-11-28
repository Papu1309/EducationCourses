using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EducationCourses.Connect
{

    public class Connection
    {
        private static EducationCoursesEntities _entities;

        public static EducationCoursesEntities entities
        {
            get
            {
                if (_entities == null)
                {
                    try
                    {
                        _entities = new EducationCoursesEntities();
                        _entities.Database.Connection.Open();
                        _entities.Database.Connection.Close();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Не удалось подключиться к базе данных: {ex.Message}");
                    }
                }
                return _entities;
            }
        }

        public static void ReloadContext()
        {
            if (_entities != null)
            {
                _entities.Dispose();
                _entities = null;
            }
        }
    }
   
}
