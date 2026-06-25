using System;
using System.Text.RegularExpressions;
using DevDeck.Contracts;
using DevDeck.Models;

namespace DevDeck.Services
{
    public sealed class VariableResolver : IVariableResolver
    {
        private static readonly Regex EnvRegex = new Regex(@"\$\{env:([^}]+)\}", RegexOptions.Compiled);

        public string Resolve(string input, ProjectEntity project)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string result = input;
            
            // Resolve project and workspace variables
            result = result.Replace("${project.path}", project.FolderPath, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("${project.name}", project.Name, StringComparison.OrdinalIgnoreCase);
            
            if (project.Workspace != null)
            {
                result = result.Replace("${workspace.name}", project.Workspace.Name, StringComparison.OrdinalIgnoreCase);
                result = result.Replace("${workspace.id}", project.WorkspaceId.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                result = result.Replace("${workspace.name}", "", StringComparison.OrdinalIgnoreCase);
                result = result.Replace("${workspace.id}", project.WorkspaceId.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            // Resolve environment variables: ${env:VAR}
            result = EnvRegex.Replace(result, m =>
            {
                string varName = m.Groups[1].Value;
                return Environment.GetEnvironmentVariable(varName) ?? string.Empty;
            });

            return result;
        }
    }
}
