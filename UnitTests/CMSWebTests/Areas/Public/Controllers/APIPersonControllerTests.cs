﻿using CmsWeb.Areas.Public.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SharedTestFixtures;
using CMSWebTests.Support;
using CMSWebTests;
using Shouldly;
using System.Web.Mvc;
using System.Web.Routing;
using CmsWeb.Membership;
using CmsWeb.Lifecycle;
using CmsData;
using System.Web;

namespace CmsWeb.Areas.Public.ControllersTests
{
    [Collection(Collections.Database)]
    public class APIPersonControllerTests : ControllerTestBase
    {
        [Fact]
        public void PortraitTest()
        {
            Person personWithPortrait = db.People.Where(p => p.PictureId != null).First();

            var requestManager = FakeRequestManager.Create(false);
            HttpRuntime.Cache.Add("imagenotfound", new Byte[] { }, null, DateTime.Now.AddHours(1), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);

            var controller = new APIPersonController(requestManager);
            var routeData = new RouteData();
            controller.ControllerContext = new ControllerContext(requestManager.CurrentHttpContext, routeData, controller);
            var result = controller.Portrait(personWithPortrait.Picture.MediumId, 100, 100);
            result.ExecuteResult(controller.ControllerContext);
            //requestManager.CurrentHttpContext.Response.StatusCode.ShouldBe(400);
            
            var username = RandomString();
            var password = RandomString();
            var user = CreateUser(username, password);

            HttpCookie AuthCookie = new HttpCookie("Authorization");
            AuthCookie.Value = BasicAuthenticationString(username, password);
            AuthCookie.Expires = DateTime.Now.AddMinutes(5);
            requestManager.CurrentHttpContext.Request.Cookies.Add(AuthCookie);

            controller = new APIPersonController(requestManager);
            routeData = new RouteData();
            controller.ControllerContext = new ControllerContext(requestManager.CurrentHttpContext, routeData, controller);
            result = controller.Portrait(personWithPortrait.Picture.MediumId, 100, 100) as HttpStatusCodeResult;
            result.ExecuteResult(controller.ControllerContext);
            //result?.StatusCode.ShouldBe(200);
        }
    }
}
