﻿using cloudscribe.SimpleContent.Models;
using cloudscribe.SimpleContent.Web.Services;
using cloudscribe.SimpleContent.Web.ViewModels;
using cloudscribe.Web.Common;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Web
{
    public class RecentPostsViewComponent : ViewComponent
    {
        public RecentPostsViewComponent(
            IProjectService projectService,
            IPostQueries postQueries,
            IContentProcessor contentProcessor,
            ITimeZoneHelper timeZoneHelper
            )
        {
            this.projectService = projectService;
            this.postQueries = postQueries;
            this.timeZoneHelper = timeZoneHelper;
            this.contentProcessor = contentProcessor;
        }

        private IProjectService projectService;
        private IPostQueries postQueries;
        private ITimeZoneHelper timeZoneHelper;
        private IContentProcessor contentProcessor;


        public async Task<IViewComponentResult> InvokeAsync(string viewName = "RecentPosts", int numberToShow = 5)
        {
            var model = new RecentPostsViewModel(contentProcessor);
            var settings = await projectService.GetCurrentProjectSettings().ConfigureAwait(false);
            var list = await postQueries.GetRecentPosts(settings.Id, numberToShow);
            model.ProjectSettings = settings;
            model.Posts = list;
            model.TimeZoneHelper = timeZoneHelper;
            model.TimeZoneId = model.ProjectSettings.TimeZoneId;

            return View(viewName, model);
        }

    }
}
