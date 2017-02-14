﻿using System;
using System.Web.Mvc;
using TMP.Core.Utility;
using Umbraco.Core.Logging;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using TMP.Core.Models;

namespace TMP.Core.Controllers
{
    public class ContactFormController : SurfaceController
    {
        private readonly MailHelper _mailHelper = new MailHelper();
        private const int FormFolderId = 1189;

        public ActionResult RenderContactForm()
        {
            return PartialView("~/Views/Partials/Forms/ContactFormView.cshtml", new ContactForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessFormSubmission(ContactForm model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ValidationFailed"] = "The form validation could not pass. Please check your input.";

                return CurrentUmbracoPage();
            }

            TempData["ValidationPasses"] = "The form has been validated successfully.";
            TempData["FormFolderId"] = FormFolderId;

            SaveContactFormSubmission(model);
            SendEmailNotifications(model);

            var formFolder = Umbraco.TypedContent(FormFolderId);

            if (formFolder != null && formFolder.HasValue("redirectPage"))
            {
                return RedirectToUmbracoPage(formFolder.GetPropertyValue<int>("redirectPage"));
            }

            return RedirectToCurrentUmbracoPage();
        }

        private void SaveContactFormSubmission(ContactForm model)
        {
            try
            {
                var contentService = Services.ContentService;
                var formSubmission = contentService.CreateContent(model.Name + ", " + model.Email + " - " + DateTime.Now.ToShortDateString(), FormFolderId, "contactForm");

                formSubmission.SetValue("name", model.Name);
                formSubmission.SetValue("emailAddress", model.Email);
                formSubmission.SetValue("message", model.Message);

                contentService.SaveAndPublishWithStatus(formSubmission);
            }
            catch (Exception ex)
            {
                LogHelper.Warn(GetType(), "Contact form saving failed with the exception: " + ex.Message);
            }

        }

        private void SendEmailNotifications(ContactForm model)
        {
            var formFolder = Umbraco.TypedContent(FormFolderId);

            if (formFolder != null)
            {
                _mailHelper.CreateAndSendNotifications(model, formFolder);
            }
            else
            {
                LogHelper.Warn(GetType(), "Couldn't get the form folder with the id: " + FormFolderId);
            }
        }
    }
}
