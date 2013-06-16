namespace CustomOAuthProvider.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Transactions;
    using System.Web.Mvc;

    using CustomOAuthProvider.Filters;
    using CustomOAuthProvider.Models;

    using Microsoft.Web.WebPages.OAuth;

    using WebMatrix.WebData;

    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public enum ManageMessageId
        {
            ChangePasswordSuccess, 
            SetPasswordSuccess, 
            RemoveLoginSuccess, 
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            this.ViewBag.ReturnUrl = returnUrl;
            return this.View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (this.ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
            {
                return this.RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            this.ModelState.AddModelError(string.Empty, "The user name or password provided is incorrect.");
            return View(model);
        }

        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return this.RedirectToAction("Index", "Home");
        }

        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            var ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == this.User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    var hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(this.User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(this.User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return this.RedirectToAction("Manage", new { Message = message });
        }

        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            this.ViewBag.StatusMessage = message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed." : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set." : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed." : string.Empty;
            this.ViewBag.ReturnUrl = this.Url.Action("Manage");
            return this.View();
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(provider, this.Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public ActionResult ExternalLoginCallback(string returnUrl)
        {
            var result = OAuthWebSecurity.VerifyAuthentication(this.Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return this.RedirectToAction("ExternalLoginFailure");
            }

            if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
            {
                return this.RedirectToLocal(returnUrl);
            }

            if (this.User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, this.User.Identity.Name);
                return this.RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                var loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
                this.ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
                this.ViewBag.ReturnUrl = returnUrl;
                return this.View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            if (this.User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return this.RedirectToAction("Manage");
            }

            if (this.ModelState.IsValid)
            {
                // Insert a new user into the database
                using (var db = new UsersContext())
                {
                    var user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());

                    // Check if user already exists
                    if (user == null)
                    {
                        // Insert name into the profile table
                        db.UserProfiles.Add(new UserProfile { UserName = model.UserName });
                        db.SaveChanges();

                        OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
                        OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

                        return this.RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        this.ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                    }
                }
            }

            this.ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
            this.ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return this.View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public ActionResult ExternalLoginsList(string returnUrl)
        {
            this.ViewBag.ReturnUrl = returnUrl;
            return this.PartialView("_ExternalLoginsListPartial", OAuthWebSecurity.RegisteredClientData);
        }

        [ChildActionOnly]
        public ActionResult RemoveExternalLogins()
        {
            var accounts = OAuthWebSecurity.GetAccountsFromUserName(this.User.Identity.Name);
            var externalLogins = new List<ExternalLogin>();
            foreach (var account in accounts)
            {
                var clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin { Provider = account.Provider, ProviderDisplayName = clientData.DisplayName, ProviderUserId = account.ProviderUserId, });
            }

            this.ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(this.User.Identity.Name));
            return this.PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (this.Url.IsLocalUrl(returnUrl))
            {
                return this.Redirect(returnUrl);
            }
            else
            {
                return this.RedirectToAction("Index", "Home");
            }
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                this.Provider = provider;
                this.ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(this.Provider, this.ReturnUrl);
            }
        }
    }
}