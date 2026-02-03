using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniParser;
using IniParser.Model;

namespace PasswordProtector.Services
{
    public class TagService
    {
        private readonly string _iniFilePath;
        private readonly FileIniDataParser _parser;
        private HashSet<string> _allTags;
        public TagService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordProtector");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _iniFilePath = Path.Combine(appDataPath, "tags.ini");
            _parser = new FileIniDataParser();
            _allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadTags();
        }

        public List<string> GetAllTags()
        {
            return _allTags.OrderBy(t => t).ToList();
        }

        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                var trimmedTag = tag.Trim();
                _allTags.Add(trimmedTag);
                SaveTags();
            }
        }

        public void RemoveTag(string tag)
        {
            _allTags.Remove(tag);
            SaveTags();
        }

        public List<string> GetSuggestions(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return GetAllTags();
            }

            var lowerInput = input.ToLower();
            return _allTags
                .Where(t => t.ToLower().Contains(lowerInput))
                .OrderBy(t => t)
                .ToList();
        }

        public void UpdateTagsFromAccounts(List<string> accountTags)
        {
            foreach (var tag in accountTags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    _allTags.Add(tag.Trim());
                }
            }
            SaveTags();
        }

        private void LoadTags()
        {
            // Add default tags if file doesn't exist
            if (!File.Exists(_iniFilePath))
            {
                var defaultTags = new[] { "업무망", "중요망", "인터넷망", "채널망", "개발", "운영" };
                foreach (var tag in defaultTags)
                {
                    _allTags.Add(tag);
                }
                SaveTags();
                return;
            }

            try
            {
                var data = _parser.ReadFile(_iniFilePath);
                var tagsSection = data.Sections["Tags"];
                
                if (tagsSection != null)
                {
                    var tagsValue = tagsSection["TagList"] ?? string.Empty;
                    if (!string.IsNullOrEmpty(tagsValue))
                    {
                        var tags = tagsValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var tag in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                _allTags.Add(tag.Trim());
                            }
                        }
                    }
                }
                // 파일이 존재하면 저장된 태그만 사용 (기본 태그 자동 추가하지 않음)
            }
            catch
            {
                // 파일 읽기 실패 시에만 기본 태그 사용
                var defaultTags = new[] { "업무망", "중요망", "인터넷망", "채널망", "개발", "운영" };
                foreach (var tag in defaultTags)
                {
                    _allTags.Add(tag);
                }
                SaveTags();
            }
        }

        private void SaveTags()
        {
            try
            {
                var data = new IniData();
                var tagList = string.Join("|", _allTags.OrderBy(t => t));
                data["Tags"]["TagList"] = tagList;
                
                _parser.WriteFile(_iniFilePath, data);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
