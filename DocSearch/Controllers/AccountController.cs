﻿using Auth0.AspNet;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IdentityModel.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace DocSearch.Controllers
{
    public class AccountController : Controller
    {
        public RedirectResult Logout()
        {
            // Clear the session cookie
            FederatedAuthentication.SessionAuthenticationModule.SignOut();
            
            // Redirect to Auth0's logout endpoint
            var returnTo = Url.Action("Index", "Home", null, protocol: Request.Url.Scheme);
            return this.Redirect(
              string.Format(CultureInfo.InvariantCulture,
                "https://{0}/v2/logout?returnTo={1}",
                ConfigurationManager.AppSettings["auth0:Domain"],
                this.Server.UrlEncode(returnTo)));
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl) || !this.Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/Home";
            }

            // you can use this for the 'authParams.state' parameter
            // in Lock, to provide a return URL after the authentication flow.
            ViewBag.State = "ru=" + HttpUtility.UrlEncode(returnUrl);

            return this.View();
        }

        [HttpGet]
        public async Task<ActionResult> LoginCallback(string code)
        {
            try
            {
                AuthenticationApiClient client = new AuthenticationApiClient(
                    new Uri(string.Format("https://{0}", ConfigurationManager.AppSettings["auth0:Domain"])));

                var token = await client.ExchangeCodeForAccessTokenAsync(new ExchangeCodeRequest
                {
                    ClientId = ConfigurationManager.AppSettings["auth0:ClientId"],
                    ClientSecret = ConfigurationManager.AppSettings["auth0:ClientSecret"],
                    AuthorizationCode = code,
                    RedirectUri = HttpContext.Request.Url.ToString()
                });

                var profile = await client.GetUserInfoAsync(token.AccessToken);

                var user = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("name", profile.UserName ?? profile.Email),
                new KeyValuePair<string, object>("email", profile.Email),
                new KeyValuePair<string, object>("family_name", profile.LastName),
                new KeyValuePair<string, object>("given_name", profile.FirstName),
                new KeyValuePair<string, object>("nickname", profile.NickName),
                new KeyValuePair<string, object>("picture", profile.Picture),
                new KeyValuePair<string, object>("user_id", profile.UserId),
                new KeyValuePair<string, object>("id_token", token.IdToken),
                new KeyValuePair<string, object>("access_token", token.AccessToken),
                new KeyValuePair<string, object>("refresh_token", token.RefreshToken),
                new KeyValuePair<string, object>("connection", profile.Identities.First().Connection),
                new KeyValuePair<string, object>("provider", profile.Identities.First().Provider)
            };

                // NOTE: Uncomment the following code in order to include claims from associated identities
                //profile.Identities.ToList().ForEach(i =>
                //{
                //    user.Add(new KeyValuePair<string, object>(i.Connection + ".access_token", i.AccessToken));
                //    user.Add(new KeyValuePair<string, object>(i.Connection + ".provider", i.Provider));
                //    user.Add(new KeyValuePair<string, object>(i.Connection + ".user_id", i.UserId));
                //});

                // NOTE: uncomment this if you send roles
                // user.Add(new KeyValuePair<string, object>(ClaimTypes.Role, profile.ExtraProperties["roles"]));

                // NOTE: this will set a cookie with all the user claims that will be converted 
                //       to a ClaimsPrincipal for each request using the SessionAuthenticationModule HttpModule. 
                //       You can choose your own mechanism to keep the user authenticated (FormsAuthentication, Session, etc.)
                FederatedAuthentication.SessionAuthenticationModule.CreateSessionCookie(user);

                if (HttpContext.Request.QueryString["state"] != null && HttpContext.Request.QueryString["state"].StartsWith("ru="))
                {
                    var state = HttpUtility.ParseQueryString(HttpContext.Request.QueryString["state"]);
                    return Redirect(state["ru"]);
                }
                return Redirect("/");
            }
            catch(Exception ex)
            {
                Trace.TraceError(ConfigurationManager.AppSettings["auth0:ClientId"]);
                Trace.TraceError(ConfigurationManager.AppSettings["auth0:ClientSecret"]);
                Trace.TraceError(code);
                Trace.TraceError(HttpContext.Request.Url.ToString());
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
                if (ex.InnerException != null)
                    Trace.TraceError(ex.InnerException.Message);
                throw;
            }            
        }
    }
}