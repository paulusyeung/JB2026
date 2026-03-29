using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace JB2026.DataAccess
{
    public class Common
    {
        #region Enums

        public class Enums
        {
            public enum Status
            {
                Suspened = -1,
                Draft = 0,
                Active,
                Power
            }

            public enum OrderType
            {
                Printing,
                PrintedLabel,
                WovenLabel,
                Other
            }

            public enum PrintFormType
            {
                JobOrder,
                PurchaseOrder
            }

            public enum EditMode
            {
                Add,
                Edit,
                Read
            }

            public enum UserRole
            {
                Guest,
                Operator,
                Supervisor,
                Manager,
                Admin
            }

            public enum Language
            {
                English = 1,
                SimplifiedChinese,
                TranditionalChinese
            }

            public enum PersonalAddress
            {
                HomeEn,
                HomeZh,
                WorkEn,
                WorkZh
            }

            public enum CorporateAddress
            {
                OfficeEn,
                OfficeZh,
                Factory
            }

            public enum WorkflowFormObject
            {
                Job_Sheet,
                Plate,
                Paper,
                Printing,
                Varnish,
                UV_Coating,
                Laminate,
                Spot_Varnish,
                Die_Cut,
                Corner,
                Punch,
                Silkscreen,
                Bronzing,
                Embossing,
                Strung_with_String,
                Pin,
                Grommet_Leyelet,
                Box,
                Sealing,
                Envelope,
                Folding,
                Book,
                Bag,
                Paper_Mount,
                Pad,
                Double_sided_Tape,
                Packing
            }

            public enum UrgencyLevel
            {
                Blue,
                Green,
                Yellow,
                Amber,
                Red
            }
        }

        #endregion

        #region Config

        public class Config
        {
            private static IConfiguration? _configuration;

            public static void Initialize(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public static string ConnectionString
            {
                get
                {
                    if (_configuration != null)
                    {
                        var cs = _configuration.GetConnectionString("SysDb");
                        if (cs != null) return cs;
                    }
                    throw new InvalidOperationException(
                        "Common.Config is not initialized. Call Common.Config.Initialize(IConfiguration) before use.");
                }
            }

            public static int SqlQueryLimit
            {
                get
                {
                    if (_configuration != null)
                    {
                        var val = _configuration["SqlQueryLimit"];
                        if (val != null) return Convert.ToInt32(val);
                    }
                    return 500;
                }
            }

            public static int CommandTimedOut
            {
                get
                {
                    if (_configuration != null)
                    {
                        var val = _configuration["CommandTimedOut"];
                        if (val != null) return Convert.ToInt32(val);
                    }
                    return 600;
                }
            }

            public static IFormatProvider DefaultCultureInfo
            {
                get { return new CultureInfo("en-US"); }
            }

            public static void SetCultureInfo(string selectedLanguage)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(selectedLanguage);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(selectedLanguage);
            }

            public static string InBox
            {
                get
                {
                    string result = @"C:\xFilm\InBox";
                    if (_configuration != null)
                    {
                        var val = _configuration["InBox"];
                        if (val != null)
                        {
                            result = val;
                            if (!Directory.Exists(result))
                                Directory.CreateDirectory(result);
                        }
                    }
                    return result;
                }
            }

            public static string OutBox
            {
                get
                {
                    string result = @"C:\xFilm\OutBox";
                    if (_configuration != null)
                    {
                        var val = _configuration["OutBox"];
                        if (val != null)
                        {
                            result = val;
                            if (!Directory.Exists(result))
                                Directory.CreateDirectory(result);
                        }
                    }
                    return result;
                }
            }

            public static string DropBox
            {
                get
                {
                    string result = @"C:\xFilm\DropBox";
                    if (_configuration != null)
                    {
                        var val = _configuration["DropBox"];
                        if (val != null)
                        {
                            result = val;
                            if (!Directory.Exists(result))
                                Directory.CreateDirectory(result);
                        }
                    }
                    return result;
                }
            }

            public static string GsWorkFolder
            {
                get
                {
                    string result = @"C:\Job.Book\WorkFolder";
                    if (_configuration != null)
                    {
                        var val = _configuration["Gswin32_WorkFolder"];
                        if (val != null)
                        {
                            result = val;
                            if (!Directory.Exists(result))
                                Directory.CreateDirectory(result);
                        }
                    }
                    return result;
                }
            }
        }

        #endregion

        #region ComboItem / ComboList

        public class ComboItem
        {
            public string Code { get; }
            public Guid Id { get; }

            public ComboItem(string code, Guid id)
            {
                Code = code;
                Id = id;
            }
        }

        public class ComboList : List<ComboItem> { }

        #endregion

        #region Utility

        public class Utility
        {
            public static bool IsGUID(string expression)
            {
                return Guid.TryParse(expression, out _);
            }

            public static bool IsNumeric(string expression)
            {
                return double.TryParse(expression, out _);
            }

            public static string RemoveSpecialCharacters(string str)
            {
                return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            }
        }

        #endregion
    }
}
