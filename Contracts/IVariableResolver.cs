using System;
using DevDeck.Models;

namespace DevDeck.Contracts
{
    public interface IVariableResolver
    {
        string Resolve(string input, ProjectEntity project);
    }
}
