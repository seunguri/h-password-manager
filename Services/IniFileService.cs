using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniParser;
using IniParser.Model;
using PasswordProtector.Models;

namespace PasswordProtector.Services
{
    public class IniFileService
    {
        private readonly string _iniFilePath;
        private readonly FileIniDataParser _parser;

        // 원본 파일 경로를 외부에서 접근할 수 있도록 프로퍼티 추가
        public string FilePath => _iniFilePath;

        public IniFileService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordProtector");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _iniFilePath = Path.Combine(appDataPath, "accounts.ini");
            _parser = new FileIniDataParser();
        }

        public List<Account> LoadAccounts()
        {
            if (!File.Exists(_iniFilePath))
            {
                return new List<Account>();
            }

            try
            {
                var data = _parser.ReadFile(_iniFilePath);
                var accounts = new List<Account>();
                var idsBackfilled = false;

                foreach (var section in data.Sections)
                {
                    // Notes의 줄바꿈 문자 디코딩 (\\n -> 실제 줄바꿈)
                    var notesRaw = data[section.SectionName]["Notes"] ?? string.Empty;
                    var notesDecoded = notesRaw.Replace("\\n", "\n").Replace("\\r", "\r");

                    var idRaw = data[section.SectionName]["Id"];
                    Guid id;
                    if (Guid.TryParse(idRaw, out var parsedId) && parsedId != Guid.Empty)
                    {
                        id = parsedId;
                    }
                    else
                    {
                        id = Guid.NewGuid();
                        idsBackfilled = true;
                    }

                    var account = new Account
                    {
                        Id = id,
                        ServiceName = AccountFieldLimits.Clamp(data[section.SectionName]["ServiceName"], AccountFieldLimits.ServiceNameMaxLength),
                        Username = data[section.SectionName]["Username"] ?? string.Empty,
                        Password = LocalSecretProtector.UnprotectFromStorage(data[section.SectionName]["Password"] ?? string.Empty),
                        Notes = AccountFieldLimits.Clamp(notesDecoded, AccountFieldLimits.NotesMaxLength),
                        Tags = data[section.SectionName]["Tags"] ?? string.Empty,
                        Order = int.TryParse(data[section.SectionName]["Order"], out var order) ? order : 0
                    };

                    if (DateTime.TryParse(data[section.SectionName]["LastPasswordChangeDate"], out var date))
                    {
                        account.LastPasswordChangeDate = date;
                    }
                    
                    if (DateTime.TryParse(data[section.SectionName]["ResetDate"], out var resetDate))
                    {
                        account.ResetDate = resetDate;
                    }
                    
                    if (int.TryParse(data[section.SectionName]["ResetPeriodDays"], out var periodDays))
                    {
                        account.ResetPeriodDays = periodDays;
                    }

                    accounts.Add(account);
                }

                var ordered = accounts.OrderBy(a => a.Order).ToList();
                if (idsBackfilled)
                {
                    try
                    {
                        SaveAccounts(ordered);
                    }
                    catch
                    {
                        // Id 백필 저장 실패 시에도 목록은 반환 (다음 저장 시 재시도)
                    }
                }

                return ordered;
            }
            catch
            {
                return new List<Account>();
            }
        }

        public void SaveAccounts(List<Account> accounts)
        {
            var data = new IniData();

            for (int i = 0; i < accounts.Count; i++)
            {
                var account = accounts[i];
                var sectionName = $"Account_{i}";
                
                account.Order = i;
                if (account.Id == Guid.Empty)
                    account.Id = Guid.NewGuid();
                account.ServiceName = AccountFieldLimits.Clamp(account.ServiceName, AccountFieldLimits.ServiceNameMaxLength);
                account.Notes = AccountFieldLimits.Clamp(account.Notes, AccountFieldLimits.NotesMaxLength);
                data[sectionName]["Id"] = account.Id.ToString("D");
                data[sectionName]["ServiceName"] = account.ServiceName;
                data[sectionName]["Username"] = account.Username ?? string.Empty;
                data[sectionName]["Password"] = LocalSecretProtector.ProtectForStorage(account.Password ?? string.Empty);
                
                // Notes의 줄바꿈 문자 인코딩 (실제 줄바꿈 -> \\n)
                var notesEncoded = (account.Notes ?? string.Empty).Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\r");
                data[sectionName]["Notes"] = notesEncoded;

                data[sectionName]["Tags"] = account.Tags ?? string.Empty;
                data[sectionName]["Order"] = account.Order.ToString();
                
                if (account.LastPasswordChangeDate.HasValue)
                {
                    data[sectionName]["LastPasswordChangeDate"] = account.LastPasswordChangeDate.Value.ToString("yyyy-MM-dd");
                }
                
                if (account.ResetDate.HasValue)
                {
                    data[sectionName]["ResetDate"] = account.ResetDate.Value.ToString("yyyy-MM-dd");
                }
                
                if (account.ResetPeriodDays.HasValue)
                {
                    data[sectionName]["ResetPeriodDays"] = account.ResetPeriodDays.Value.ToString();
                }
            }

            _parser.WriteFile(_iniFilePath, data);
        }
    }
}
