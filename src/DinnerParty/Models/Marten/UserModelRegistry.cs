using System;
using Marten;

namespace DinnerParty.Models.Marten
{
    public class UserModelRegistry : MartenRegistry
    {
        public UserModelRegistry()
        {
            // Generate a searchable index for UserModel.Username
            For<UserModel>().Searchable(u => u.Username);
            // Generate a searchable index for UserModel.EMailAddress
            For<UserModel>().Searchable(u => u.EMailAddress);
        }
    }
}