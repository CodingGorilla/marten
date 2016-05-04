﻿using System;
using System.Linq;
using Nancy.Authentication.Forms;
using Nancy.Validation;
using Nancy.ModelBinding;
using DinnerParty.Models;
using System.Configuration;
using System.Net;
using DinnerParty.Models.Marten;
using Marten;
using Newtonsoft.Json;

namespace DinnerParty.Modules
{
    public class AccountModule : BaseModule
    {
        public AccountModule(IDocumentSession documentSession)
            : base("/account")
        {
            Get["/logon"] = parameters =>
            {
                base.Page.Title = "Login";

                var loginModel = new LoginModel();
                base.Model.LoginModel = loginModel;

                return View["LogOn", base.Model];
            };

            Post["/logon"] = parameters =>
                {
                    var model = this.Bind<LoginModel>();
                    var result = this.Validate(model);

                    var userMapper = new UserMapper(documentSession);
                    var userGuid = userMapper.ValidateUser(model.UserName, model.Password);

                    if (userGuid == null || !result.IsValid)
                    {
                        base.Page.Title = "Login";

                        foreach (var item in result.FormattedErrors)
                        {
                            foreach (var member in item.MemberNames)
                            {
                                base.Page.Errors.Add(new ErrorModel() { Name = member, ErrorMessage = item.GetMessage(member) });
                            }
                        }

                        if (userGuid == null && base.Page.Errors.Count == 0)
                            base.Page.Errors.Add(new ErrorModel() { Name = "UserName", ErrorMessage = "Unable to find user" });


                        base.Model.LoginModel = model;

                        return View["LogOn", base.Model];
                    }

                    DateTime? expiry = null;
                    if (model.RememberMe)
                    {
                        expiry = DateTime.Now.AddDays(7);
                    }

                    return this.LoginAndRedirect(userGuid.Value, expiry);
                };

            Get["/logoff"] = parameters =>
                {
                    return this.LogoutAndRedirect("/");
                };

            Get["/register"] = parameters =>
            {
                base.Page.Title = "Register";

                var registerModel = new RegisterModel();
                base.Model.RegisterModel = registerModel;


                return View["Register", base.Model];
            };

            Post["/register"] = parameters =>
                {
                    var model = this.Bind<RegisterModel>();
                    var result = this.Validate(model);

                    if (!result.IsValid)
                    {
                        base.Page.Title = "Register";

                        base.Model.RegisterModel = model;

                        foreach (var item in result.FormattedErrors)
                        {
                            foreach (var member in item.MemberNames)
                            {
                                base.Page.Errors.Add(new ErrorModel() { Name = member, ErrorMessage = item.GetMessage(member) });
                            }
                        }

                        return View["Register", base.Model];
                    }

                    var userMapper = new UserMapper(documentSession);
                    var userGUID = userMapper.ValidateRegisterNewUser(model);

                    //User already exists
                    if (userGUID == null)
                    {
                        base.Page.Title = "Register";
                        base.Model.RegisterModel = model;
                        base.Page.Errors.Add(new ErrorModel() { Name = "EmailAddress", ErrorMessage = "This email address has already been registered" });
                        return View["Register", base.Model];
                    }

                    DateTime? expiry = DateTime.Now.AddDays(7);

                    return this.LoginAndRedirect(userGUID.Value, expiry);
                };



            Post["/token"] = parameters =>
            {
                string Apikey = ConfigurationManager.AppSettings["JanrainKey"];

                if (string.IsNullOrWhiteSpace(Request.Form.token))
                {
                    base.Page.Title = "Login Error";
                    base.Model.LoginModel = "Bad response from login provider - could not find login token.";

                    return View["Error", base.Model];
                }

                var response = new WebClient().DownloadString(string.Format("https://rpxnow.com/api/v2/auth_info?apiKey={0}&token={1}", Apikey, Request.Form.token));

                if (string.IsNullOrWhiteSpace(response))
                {
                    base.Page.Title = "Login Error";
                    base.Model.LoginModel = "Bad response from login provider - could not find user.";
                    return View["Error", base.Model];
                }

                var j = JsonConvert.DeserializeObject<dynamic>(response);

                if (j.stat.ToString() != "ok")
                {
                    base.Page.Title = "Login Error";
                    base.Model.LoginModel = "Bad response from login provider - could not find login token.";
                    return View["Error", base.Model];
                }

                string userIdentity = j.profile.identifier.ToString();
                string displayName = j.profile.displayName.ToString();
                string username = j.profile.preferredUsername.ToString();
                string email = string.Empty;
                if (j.profile.email != null)
                    email = j.profile.email.ToString();

                var user = documentSession.Query<UserModel>().FirstOrDefault(x => x.LoginType == userIdentity);
                              
                if (user == null)
                {
                    UserModel newUser = new UserModel()
                    {
                        Id = Guid.NewGuid(),
                        EMailAddress = (!string.IsNullOrEmpty(email)) ? email : "none@void.com",
                        Username = (!string.IsNullOrEmpty(username)) ? username : "New User " + documentSession.Query<UserModel>().Count(),
                        LoginType = userIdentity,
                        FriendlyName = displayName
                    };

                    documentSession.Store(newUser);
                    documentSession.SaveChanges();
                    return this.LoginAndRedirect(newUser.Id, DateTime.Now.AddDays(7));
                }

                return this.LoginAndRedirect(user.Id, DateTime.Now.AddDays(7));
            };
        }
    }
}