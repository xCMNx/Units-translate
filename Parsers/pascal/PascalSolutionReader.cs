using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Core;

namespace pascal
{
    [MapperSolutionFilter("Delphi", new[] { "groupproj", "dproj", "dpr" })]
    class PascalSolutionReader : ISolutionReader
    {

        string GetFilePath(string file, string rootDir)
        {
            var path = Path.GetFullPath(file);
            if (!File.Exists(path))
            {
                var oldpath = path;
                path = Path.Combine(rootDir, path.Substring(3));
                if (!File.Exists(path))
                {
                    Helpers.ConsoleWrite($"File is not exists: {file}");
                    return string.Empty;
                }
                //Helpers.ConsoleWrite($"File \"{oldpath}\" restored to \"{path}\"");
            }
            return path;
        }

        static Regex r2 = new Regex(@"(?ixms)
 \{.*?\}
|\(\*.*?\*\)
|//.*?$
|\w+\s+in\s+'(?<file>[^'\0\r\n]*|''*)?'
|'(?:[^'\0\r\n]*|''*)?'
|(?<name>\w+)
");

        ICollection<string> GetProjFiles(string projFileName, string mainDir)
        {
            var dir = Directory.GetCurrentDirectory();
            var path = GetFilePath(projFileName, mainDir);
            var files = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            if (File.Exists(path))
            {
                files.Add(path);
                try
                {
                    var rootDir = Path.GetDirectoryName(path);
                    Directory.SetCurrentDirectory(rootDir);
                    var matches = PascalMapper.regexUses.Matches(File.ReadAllText(path));
                    foreach (Match m in matches)
                        foreach (Match m2 in r2.Matches(m.Groups[2].Value))
                        {
                            if (!string.IsNullOrWhiteSpace(m2.Groups[1].Value))
                            {
                                var fpath = GetFilePath(m2.Groups[1].Value, mainDir);
                                if (File.Exists(fpath))
                                    files.Add(fpath);
                            }
                            else if (!string.IsNullOrWhiteSpace(m2.Groups[2].Value))
                            {
                                var fpath = Path.GetFullPath($"{m2.Groups[2].Value}.pas");
                                if (File.Exists(fpath))
                                {
                                    files.Add(fpath);
                                    fpath = Path.ChangeExtension(fpath, ".dfm");
                                    if (File.Exists(fpath))
                                        files.Add(fpath);
                                }
                            }
                        }
                }
                finally
                {
                    Directory.SetCurrentDirectory(dir);
                }
            }
            return files;
        }

        ICollection<string> GetProjectFiles(string dprojFileName)
        {
            var dir = Directory.GetCurrentDirectory();
            var path = GetFilePath(dprojFileName, dir);
            var files = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            if (File.Exists(path))
            {
                files.Add(path);
                try
                {
                    var rootDir = Path.GetDirectoryName(path);
                    Directory.SetCurrentDirectory(rootDir);
                    var x = XmlToDynamic.Parse(File.ReadAllText(path));
                    try
                    {
                        var references = x.ItemGroup.DCCReference;
                        foreach (var reference in references)
                        {
                            var fpath = GetFilePath(reference.Include, dir);
                            if (File.Exists(fpath))
                            {
                                files.Add(fpath);
                                fpath = Path.ChangeExtension(fpath, ".dfm");
                                if (File.Exists(fpath))
                                    files.Add(fpath);
                            }
                        }
                    }
                    catch { }
                    try
                    {
                        var project = GetFilePath(x?.ItemGroup?.DelphiCompile.Include, dir);
                        if (File.Exists(project))
                        {
                            files.Add(project);
                            foreach(var f in GetProjFiles(project, dir))
                                files.Add(f);
                        }
                    }
                    catch { }
                }
                finally
                {
                    Directory.SetCurrentDirectory(dir);
                }
            }
            return files;
        }

        ICollection<string> GetGroupProjFiles(string groupProjFileName)
        {
            var files = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var x = XmlToDynamic.Parse(File.ReadAllText(groupProjFileName));
            try
            {
                var projects = x.ItemGroup.Projects;
                foreach (var proj in projects)
                    foreach (var f in GetProjectFiles(Path.GetFullPath(proj.Include)))
                        files.Add(f);
                files.Add(groupProjFileName);
            }
            catch { }
            return files;
        }

        public ICollection<string> GetFiles(string solutionFileName)
        {
            var dir = Directory.GetCurrentDirectory();
            try
            {
                var mDir = Path.GetDirectoryName(Path.GetFullPath(solutionFileName));
                Directory.SetCurrentDirectory(mDir);
                switch (Path.GetExtension(solutionFileName).ToLower())
                {
                    case ".groupproj": return GetGroupProjFiles(solutionFileName);
                    case ".dproj": return GetGroupProjFiles(solutionFileName);
                    case ".dpr": return GetProjFiles(solutionFileName, mDir);
                }
                return new string[0];
            }
            finally
            {
                Directory.SetCurrentDirectory(dir);
            }
        }
    }
}
