﻿using System.Reflection;
using System.Security;
using System.Security.Principal;
using Scorpion_Hasher_Library;
using ScorpionAES;
using SquirrelDefaultPaths;

namespace Scorpion_Authenticator
{
    public class Authenticator
    {
        string System_user;
        string full_path = SquirrelPaths.main_users_config_path;
        string full_directory_path = SquirrelPaths.main_users_path;
        string[] Config_file_content;

        public Authenticator()
        {
            check();
            return;
        }

        public bool authenticate(ref string User, SecureString Passcode)
        {
            string[] elements;
            string[] sep = { "@@@~" };
            if (Config_file_content.Length == 0 || Config_file_content[0] == "")
                return false;

            foreach(string line in Config_file_content)
            {
                elements = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (elements[0] == User)
                    return new Scorpion_Hasher().Bverify(Passcode, elements[1]);
            }
            return false;
        }

        private bool checkExists(string User)
        {
            bool retval = false;
            string[] elements;
            string[] sep = { "@@@~" };

            if (Config_file_content.Length == 0 || Config_file_content[0] == "")
                return false;

            foreach(string line in Config_file_content)
            {
                elements = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (elements[0] == User)
                    retval = true;
            }
            return retval;
        }

        public void create_user(MethodInfo[] scorpion_functions)
        {
            Console.Write("New username >> ");
            string uname = Console.ReadLine();
            Console.Write("New password >> ");
            SecureString pwd = read_password();

            //Does the user exist if so then return
            if(checkExists(uname))
            {
                Console.WriteLine("User exists, please choose a different username");
                return;
            }

            write_config(ref uname, ref pwd);
            ExecutionPersmissions ep = new ExecutionPersmissions(ref uname);
            ep.create(ref uname, scorpion_functions);
            read_config();
            return;
        }

        private void get_system_user()
        {
            System_user = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return;
        }

        private void create_path()
        {
            return;
        }

        public void check()
        {
            get_system_user();
            create_path();
            check_directory();
            check_configuration();
            read_config();
            return;
        }

        private void check_directory()
        {
            if (!Directory.Exists(full_directory_path))
                Directory.CreateDirectory(full_directory_path);
            return;
        }

        private void check_configuration()
        {
            if (!File.Exists(full_path))
                File.Create(full_path).Close();
            return;
        }

        private void read_config()
        {
            Config_file_content = File.ReadAllLines(full_path, System.Text.Encoding.UTF8);
            return;
        }

        private void write_config(ref string uname, ref SecureString pwd)
        {
            Scorpion_Hasher sch = new Scorpion_Hasher();
            StreamWriter sr = File.AppendText(full_path);
            Console.WriteLine("Creating user...");
            sr.WriteLine("@@@~" + uname + "@@@~" + sch.Bhash(pwd) + "@@@~");
            sr.Flush();
            sr.Close();
            return;
        }

        private void delete_from_config(ref string uname)
        {
            read_config(); int ndx = 0;
            foreach(string s_accnt in Config_file_content)
            {
                if (s_accnt.StartsWith("@@@~" + uname + "@@@~", StringComparison.CurrentCulture))
                {
                    Config_file_content.SetValue("", ndx);
                    File.WriteAllLines(full_path, Config_file_content);
                    break;
                }
                ndx++;
            }
            return;
        }

        public SecureString read_password()
        {
            //Do not append to string, only directl to Secure string or char/byte array.

            ConsoleKeyInfo cki;
            SecureString s_pwd = new SecureString();
            do
            {
                cki = Console.ReadKey(true);
                if (cki.Key != ConsoleKey.Enter)
                    s_pwd.AppendChar(cki.KeyChar);
            }
            while (cki.Key != ConsoleKey.Enter);
            return s_pwd;
        }
    }

    public class ExecutionPersmissions
    {
        string[] Config_file_content;
        string System_user, full_path, full_directory_path = SquirrelPaths.main_users_path;
        protected string user = null;
        static string scorpion_directory = SquirrelPaths.main_users_path; //"Scorpion/Users";
        static string scorpion_config = SquirrelPaths.main_users_perm_config_path; //"_perm.conf";

        public ExecutionPersmissions(ref string user)
        {
            this.user = user;
            full_path = String.Format(SquirrelPaths.main_users_perm_config_path, user);
            check();
            return;
        }

        public void write_permissions()
        {
            Console.Write(File.ReadAllText(full_path));
        }

        private void setPermissionConfigPath()
        {
            System_user = String.Format(SquirrelPaths.main_users_perm_config_path, Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            return;
        }

        public void check()
        {
            setPermissionConfigPath();
            check_directory();
            check_configuration();
            read_config();
            return;
        }

        private void check_directory()
        {
            if (!Directory.Exists(full_directory_path))
                Directory.CreateDirectory(full_directory_path);
            return;
        }

        private void check_configuration()
        {
            if (!File.Exists(full_path))
                File.Create(full_path).Close();
            return;
        }

        private void read_config()
        {
            Config_file_content = File.ReadAllLines(full_path, System.Text.Encoding.UTF8);
            return;
        }

        public void create(ref string user_, MethodInfo[] scorpion_functions)
        {
            write_permissions(scorpion_functions);
            return;
        }

        public bool check_authentication(ref string user_, ref string function)
        {
            user = user_;
            return authenticate(ref function);
        }

        private void write_permissions(MethodInfo[] scorpion_functions)
        {
            read_config();

            //Crashes if the permissions file already exists!!!
            Scorpion_Hasher sch = new Scorpion_Hasher();
            StreamWriter sr = File.AppendText(full_path);

            //Hash is null if no permission, Hash is same name as function if exists
            Console.WriteLine("Grant all privileges to user? [y/n]");
            string s_ans = Console.ReadLine().ToLower();
            if (s_ans == "y")
            {
                sr.WriteLine("ALL");
                Console.WriteLine("All execution permissions granted to the new user");
            }
            else
            {
                Console.WriteLine("Would you like to selectively grant the execution of specific functions? [y/n]");
                string res = Console.ReadLine().ToLower();

                if (res == "y")
                {
                    foreach (MethodInfo mthd_inf in scorpion_functions)
                    {
                        Console.WriteLine("Grant execution of {0}? [y/(n or enter)]", mthd_inf.Name);
                        res = Console.ReadLine();
                        if (res.ToLower() == "y")
                        {
                            sr.WriteLine(mthd_inf.Name);
                            Console.WriteLine("{0} executable", mthd_inf.Name);
                        }
                        else
                            Console.WriteLine("{0} not executable", mthd_inf.Name);
                    }
                }
                else
                    Console.WriteLine("No execution permissions granted to the new user");
            }
            sr.Flush();
            sr.Close();
            File.SetAttributes(full_path, FileAttributes.ReadOnly);
            Console.WriteLine("Permissions saved. Please make sure to set the correct OS file permissions for file ownership to: " + full_path + " as this file should be non writable by non administrators, the file has been changed to readonly by the current user. It could still be writable by non administrators");
            return;
        }

        private bool authenticate(ref string function)
        {
            //create_path();
            read_config();
            if (Config_file_content.Length > 0)
            {
                if (Config_file_content[0] == "ALL")
                    return true;
                //Find permission
                foreach (string s_function in Config_file_content)
                {
                    if (s_function == function)
                        return true;
                }
            }
            return false;
        }
    }

    public static class ToLines
    {
        public static string[] stringToLines(string str)
        {
            return str.Split(new [] { '\r', '\n' });
        }
    }
}
