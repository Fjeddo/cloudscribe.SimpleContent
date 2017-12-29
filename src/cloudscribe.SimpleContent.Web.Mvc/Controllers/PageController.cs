﻿// Copyright (c) Source Tree Solutions, LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Author:                  Joe Audette
// Created:                 2016-02-24
// Last Modified:           2017-11-21
// 

using cloudscribe.SimpleContent.Models;
using cloudscribe.SimpleContent.Web.Services;
using cloudscribe.SimpleContent.Web.ViewModels;
using cloudscribe.Web.Common;
using cloudscribe.Web.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace cloudscribe.SimpleContent.Web.Mvc.Controllers
{
    public class PageController : Controller
    {
        public PageController(
            IProjectService projectService,
            IPageService blogService,
            IContentProcessor contentProcessor,
            IPageRoutes pageRoutes,
            IAuthorizationService authorizationService,
            ITimeZoneHelper timeZoneHelper,
            IAuthorNameResolver authorNameResolver,
            IStringLocalizer<SimpleContent> localizer,
            IOptions<PageEditOptions> pageEditOptionsAccessor,
            ILogger<PageController> logger)
        {
            this.projectService = projectService;
            this.pageService = blogService;
            this.contentProcessor = contentProcessor;
            this.authorizationService = authorizationService;
            this.authorNameResolver = authorNameResolver;
            this.timeZoneHelper = timeZoneHelper;
            this.pageRoutes = pageRoutes;
            editOptions = pageEditOptionsAccessor.Value;
            sr = localizer;
            log = logger;
        }

        private IProjectService projectService;
        private IPageService pageService;
        private IContentProcessor contentProcessor;
        private IAuthorizationService authorizationService;
        private IAuthorNameResolver authorNameResolver;
        private ITimeZoneHelper timeZoneHelper;
        private ILogger log;
        private IPageRoutes pageRoutes;
        private IStringLocalizer<SimpleContent> sr;
        private PageEditOptions editOptions;

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string slug = "")
        {
            var projectSettings = await projectService.GetCurrentProjectSettings();

            if (projectSettings == null)
            {
                log.LogError("project settings not found returning 404");
                return NotFound();
            }

            var canEdit = await User.CanEditPages(projectSettings.Id, authorizationService);

            if(string.IsNullOrEmpty(slug) || slug == "none") { slug = projectSettings.DefaultPageSlug; }

            IPage page = await pageService.GetPageBySlug(slug);
            
            var model = new PageViewModel(contentProcessor);
            model.CurrentPage = page;
            model.ProjectSettings = projectSettings;
            model.CanEdit = canEdit;
            model.CommentsAreOpen = false;
            model.TimeZoneHelper = timeZoneHelper;
            model.TimeZoneId = model.ProjectSettings.TimeZoneId;
            model.PageTreePath = Url.Action("Tree");
            if (canEdit)
            {
                if (model.CurrentPage != null)
                {
                    model.EditPath = Url.RouteUrl(pageRoutes.PageEditRouteName, new { slug = model.CurrentPage.Slug });

                    if (model.CurrentPage.Slug == projectSettings.DefaultPageSlug)
                    {   
                       // not setting the parent slug if the current page is home page
                       // otherwise it would be awkward to create more root level pages
                        model.NewItemPath = Url.RouteUrl(pageRoutes.PageEditRouteName, new { slug = "" });
                    }
                    else
                    {
                        // for non home pages if the use clicks the new link
                        // make it use the current page slug as the parent slug for the new item
                        model.NewItemPath = Url.RouteUrl(pageRoutes.PageEditRouteName, new { slug = "", parentSlug = model.CurrentPage.Slug });

                    }

                }
                else
                {
                    model.NewItemPath = Url.RouteUrl(pageRoutes.PageEditRouteName, new { slug = "" });
                   
                }

            }

            if (page == null)
            { 
                var rootList = await pageService.GetRootPages().ConfigureAwait(false);
                // a site starts out with no pages 
                if (canEdit && rootList.Count == 0)
                {
                    page = new Page();
                    page.ProjectId = projectSettings.Id;
                    if(HttpContext.Request.Path == "/")
                    {
                        ViewData["Title"] = sr["Home"];
                        page.Title = "No pages found, please click the pencil to create the home page";
                    }
                    else
                    {
                        ViewData["Title"] = sr["No Pages Found"];
                        page.Title = "No pages found, please click the pencil to create the first page";
                        
                    }

                    
                    
                    model.CurrentPage = page;
                    model.EditPath = Url.RouteUrl(pageRoutes.PageEditRouteName, new { slug = "home" });
                    
                }
                else
                {
                    
                    if(rootList.Count > 0)
                    {
                        if(slug == projectSettings.DefaultPageSlug)
                        {
                            // slug was empty and no matching page found for default slug
                            // but since there exist root level pages we should
                            // show an index menu. 
                            // esp useful if not using pages as the default route
                            // /p or /docs
                            ViewData["Title"] = sr["Content Index"];
                            //model.EditorSettings.EditMode = "none";
                            return View("IndexMenu", model);
                        }

                        
                        return NotFound();
                    }
                    else
                    {
                        Response.StatusCode = 404;
                        return View("NoPages", 404);
                    }    
                    
                }
            }
            else // page is not null
            {
                // if the page is protected by view roles return 404 if user is not in an allowed role
                if ((!string.IsNullOrEmpty(page.ViewRoles)))
                {
                    if (!User.IsInRoles(page.ViewRoles))
                    {
                        log.LogWarning($"page {page.Title} is protected by roles that user is not in so returning 404");
                        return NotFound();
                    }
                }

                if (!canEdit)
                { 
                    if(!page.IsPublished || page.PubDate > DateTime.UtcNow)
                    {
                        log.LogWarning($"page {page.Title} is unpublished and user is not editor so returning 404");
                        return NotFound();
                    }
                }
                
                
                if(!string.IsNullOrEmpty(page.ExternalUrl))
                {
                    if(canEdit)
                    {
                        log.LogWarning($"page {page.Title} has override url {page.ExternalUrl}, redirecting to edit since user can edit");
                        return RedirectToRoute(pageRoutes.PageEditRouteName, new { slug= page.Slug });

                    }
                    else
                    {
                        log.LogWarning($"page {page.Title} has override url {page.ExternalUrl}, not intended to be viewed so returning 404");
                        return NotFound();
                    }
                }

                ViewData["Title"] = page.Title;
                
            }

            if (page != null && page.MenuOnly)
            {
                return View("ChildMenu", model);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Edit(
            string slug = "",
            string parentSlug = "",
            string type =""
            )
        {
            var projectSettings = await projectService.GetCurrentProjectSettings();

            if (projectSettings == null)
            {
                log.LogInformation("redirecting to index because project settings not found");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canEdit = await User.CanEditPages(projectSettings.Id, authorizationService);
            if(!canEdit)
            {
                log.LogInformation("redirecting to index because user cannot edit");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            if (slug == "none") { slug = string.Empty; }

            var model = new PageEditViewModel();
            model.ProjectId = projectSettings.Id;
            model.DisqusShortname = projectSettings.DisqusShortName;
           
            IPage page = null;
            if (!string.IsNullOrEmpty(slug))
            {
                page = await pageService.GetPageBySlug(slug);
            }
            if(page == null)
            {
                ViewData["Title"] = sr["New Page"];
                model.Slug = slug;
                model.ParentSlug = parentSlug;
                model.PageOrder = await pageService.GetNextChildPageOrder(parentSlug);
                model.ContentType = projectSettings.DefaultContentType;
                if (editOptions.AllowMarkdown && !string.IsNullOrWhiteSpace(type) && type == "markdown")
                {
                    model.ContentType = "markdown";
                }
                if (!string.IsNullOrWhiteSpace(type) && type == "html")
                {
                    model.ContentType = "html";
                }

                var rootList = await pageService.GetRootPages().ConfigureAwait(false);
                if(rootList.Count == 0)
                {
                    var rootPagePath = Url.RouteUrl(pageRoutes.PageRouteName);
                    // expected if home page doesn't exist yet
                    if(slug == "home" && rootPagePath == "/home")
                    {
                        model.Title = sr["Home"];
                    }

                }
                model.Author = await authorNameResolver.GetAuthorName(User);
                model.PubDate = timeZoneHelper.ConvertToLocalTime(DateTime.UtcNow, projectSettings.TimeZoneId).ToString();

            }
            else // page not null
            {
                // if the page is protected by view roles return 404 if user is not in an allowed role
                if ((!string.IsNullOrEmpty(page.ViewRoles)))
                {
                    if (!User.IsInRoles(page.ViewRoles))
                    {
                        log.LogWarning($"page {page.Title} is protected by roles that user is not in so returning 404");
                        return NotFound();
                    }
                }

                ViewData["Title"] = string.Format(CultureInfo.CurrentUICulture, sr["Edit - {0}"], page.Title);
                model.Author = page.Author;
                model.Content = page.Content;
                model.Id = page.Id;
                model.CorrelationKey = page.CorrelationKey;
                model.IsPublished = page.IsPublished;
                model.ShowMenu = page.ShowMenu;
                model.MenuOnly = page.MenuOnly;
                model.MetaDescription = page.MetaDescription;
                model.PageOrder = page.PageOrder;
                model.ParentId = page.ParentId;
                model.ParentSlug = page.ParentSlug;
                model.PubDate = timeZoneHelper.ConvertToLocalTime(page.PubDate, projectSettings.TimeZoneId).ToString();
                model.ShowHeading = page.ShowHeading;
                model.Slug = page.Slug;
                model.ExternalUrl = page.ExternalUrl;
                model.Title = page.Title;
                model.MenuFilters = page.MenuFilters;
                model.ViewRoles = page.ViewRoles;
                model.ShowComments = page.ShowComments;
                model.DisableEditor = page.DisableEditor;
                model.ContentType = page.ContentType;

            }

            
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PageEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if(string.IsNullOrEmpty(model.Id))
                {
                    ViewData["Title"] = sr["New Page"];
                }
                else
                {
                    ViewData["Title"] = string.Format(CultureInfo.CurrentUICulture, sr["Edit - {0}"], model.Title);
                }
                return View(model);
            }
            
            var project = await projectService.GetCurrentProjectSettings();
            
            if (project == null)
            {
                log.LogInformation("redirecting to index because project settings not found");

                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("redirecting to index because user is not allowed to edit");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            IPage page = null;
            if (!string.IsNullOrEmpty(model.Id))
            {
                page = await pageService.GetPage(model.Id);
            }

            var needToClearCache = false;
            var isNew = false;
            string slug = string.Empty; ;
            bool slugIsAvailable = false;
            if (page != null)
            {
                if ((!string.IsNullOrEmpty(page.ViewRoles)))
                {
                    if (!User.IsInRoles(page.ViewRoles))
                    {
                        log.LogWarning($"page {page.Title} is protected by roles that user is not in so redirecting");
                        return RedirectToRoute(pageRoutes.PageRouteName);
                    }
                }

                if (page.Title != model.Title)
                {
                    needToClearCache = true;
                }
                page.Title = model.Title;
                page.MetaDescription = model.MetaDescription;
                page.Content = model.Content;
                if (page.IsPublished != model.IsPublished) needToClearCache = true;
                if (page.PageOrder != model.PageOrder) needToClearCache = true;
                if(!string.IsNullOrEmpty(model.Slug))
                {
                    // remove any bad characters
                    model.Slug = ContentUtils.CreateSlug(model.Slug);
                    if(model.Slug != page.Slug)
                    {
                        slugIsAvailable = await pageService.SlugIsAvailable(model.Slug);
                        if(slugIsAvailable)
                        {
                            page.Slug = model.Slug;
                            needToClearCache = true;
                        }
                        else
                        {
                            this.AlertDanger(sr["The page slug was not changed because the requested slug is already in use."], true);

                        }
                    }

                }

            }
            else
            {
                isNew = true;
                needToClearCache = true;
                if(!string.IsNullOrEmpty(model.Slug))
                {
                    // remove any bad chars
                    model.Slug = ContentUtils.CreateSlug(model.Slug);
                    slugIsAvailable = await pageService.SlugIsAvailable(model.Slug);
                    if(slugIsAvailable)
                    {
                        slug = model.Slug;
                    }
                }

                if(string.IsNullOrEmpty(slug))
                {
                    slug = ContentUtils.CreateSlug(model.Title);
                }

                slugIsAvailable = await pageService.SlugIsAvailable(slug);
                if (!slugIsAvailable)
                {
                    model.DisqusShortname = project.DisqusShortName;
                    //log.LogInformation("returning 409 because slug already in use");
                    ModelState.AddModelError("pageediterror", sr["slug is already in use."]);

                    return View(model);
                }

                page = new Page()
                {
                    ProjectId = project.Id,
                    Author = await authorNameResolver.GetAuthorName(User),
                    Title = model.Title,
                    MetaDescription = model.MetaDescription,
                    Content = model.Content,
                    Slug = slug,
                    ParentId = "0"

                    //,Categories = categories.ToList()
                };
            }


            if (!string.IsNullOrEmpty(model.ParentSlug))
            {
                var parentPage = await pageService.GetPageBySlug(model.ParentSlug);
                if (parentPage != null)
                {
                    if (parentPage.Id != page.ParentId)
                    {
                        page.ParentId = parentPage.Id;
                        page.ParentSlug = parentPage.Slug;
                        needToClearCache = true;
                    }

                }
            }
            else
            {
                // empty means root level
                page.ParentSlug = string.Empty;
                page.ParentId = "0";
            }
            if (page.ViewRoles != model.ViewRoles)
            {
                needToClearCache = true;
            }
            page.ViewRoles = model.ViewRoles;
            page.CorrelationKey = model.CorrelationKey;

            page.PageOrder = model.PageOrder;
            page.IsPublished = model.IsPublished;
            page.ShowHeading = model.ShowHeading;
            page.ShowMenu = model.ShowMenu;
            page.MenuOnly = model.MenuOnly;
            page.DisableEditor = model.DisableEditor;
            page.ShowComments = model.ShowComments;
            if (page.MenuFilters != model.MenuFilters)
            {
                needToClearCache = true;
            }
            page.MenuFilters = model.MenuFilters;
            if (page.ExternalUrl != model.ExternalUrl)
            {
                needToClearCache = true;
            }
            page.ExternalUrl = model.ExternalUrl;
            page.ContentType = model.ContentType;

            if(!string.IsNullOrEmpty(model.Author))
            {
                page.Author = model.Author;
            }

            if (!string.IsNullOrEmpty(model.PubDate))
            {
                var localTime = DateTime.Parse(model.PubDate);
                page.PubDate = timeZoneHelper.ConvertToUtc(localTime, project.TimeZoneId);

            }
            if(page.ProjectId != project.Id)
            {
                page.ProjectId = project.Id;
            }

            if (isNew)
            {
                await pageService.Create(page, model.IsPublished);
                this.AlertSuccess(sr["The page was created successfully."], true);
            }
            else
            {
                await pageService.Update(page, model.IsPublished);
                this.AlertSuccess(sr["The page was updated successfully."], true);
            }


            if (needToClearCache)
            {
                pageService.ClearNavigationCache();
            }

            

            if (page.Slug == project.DefaultPageSlug)
            {
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug="" });
            }

            if(!string.IsNullOrEmpty(page.ExternalUrl))
            {
                this.AlertWarning(sr["Note that since this page has an override url, the menu item will link to the url so the page is used only as a means to add a link in the menu, the content is not used."], true);
                return RedirectToRoute(pageRoutes.PageEditRouteName, new { slug = page.Slug });
            }

            //var url = Url.RouteUrl(pageRoutes.PageRouteName, new { slug = page.Slug });
            return RedirectToRoute(pageRoutes.PageRouteName, new { slug = page.Slug });
            //return Content(url);

        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Development(string slug)
        {
            var projectSettings = await projectService.GetCurrentProjectSettings();

            if (projectSettings == null)
            {
                log.LogInformation("redirecting to index because project settings not found");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canEdit = await User.CanEditPages(projectSettings.Id, authorizationService);
            if (!canEdit)
            {
                log.LogInformation("redirecting to index because user cannot edit");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canDev = editOptions.AlwaysShowDeveloperLink ? true : User.IsInRole(editOptions.DeveloperAllowedRole);

            if (!canDev)
            {
                log.LogInformation("redirecting to index because user is not allowed by edit config for developer tools");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            IPage page = null;
            if (!string.IsNullOrEmpty(slug))
            {
                page = await pageService.GetPageBySlug(slug);
            }
            if (page == null)
            {
                log.LogInformation("page not found, redirecting");
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            if ((!string.IsNullOrEmpty(page.ViewRoles)))
            {
                if (!User.IsInRoles(page.ViewRoles))
                {
                    log.LogWarning($"page {page.Title} is protected by roles that user is not in so redirecting");
                    return RedirectToRoute(pageRoutes.PageRouteName);
                }
            }

            ViewData["Title"] = string.Format(CultureInfo.CurrentUICulture, sr["Developer Tools - {0}"], page.Title);

            var model = new PageDevelopmentViewModel();
            model.Slug = page.Slug;
            model.AddResourceViewModel.Slug = page.Slug;
            model.Css = page.Resources.Where(x => x.Type == "css").OrderBy(x => x.Sort).ThenBy(x => x.Url).ToList<IPageResource>();
            model.Js = page.Resources.Where(x => x.Type == "js").OrderBy(x => x.Sort).ThenBy(x => x.Url).ToList<IPageResource>();
            

            return View(model);

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResource(AddPageResourceViewModel model)
        {
            if(!ModelState.IsValid)
            {
                this.AlertDanger(sr["Invalid request"], true);
                return RedirectToRoute(pageRoutes.PageDevelopRouteName, new { slug = model.Slug });
            }

            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("redirecting to index because project settings not found");

                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);
            if (!canEdit)
            {
                log.LogInformation("redirecting to index because user is not allowed to edit");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }
            var canDev = editOptions.AlwaysShowDeveloperLink ? true : User.IsInRole(editOptions.DeveloperAllowedRole);
            if (!canDev)
            {
                log.LogInformation("redirecting to index because user is not allowed by edit config for developer tools");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            IPage page = null;
            if (!string.IsNullOrEmpty(model.Slug))
            {
                page = await pageService.GetPageBySlug(model.Slug);
            }

            if (page == null)
            {
                log.LogInformation("page not found, redirecting");
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            if ((!string.IsNullOrEmpty(page.ViewRoles)))
            {
                if (!User.IsInRoles(page.ViewRoles))
                {
                    log.LogWarning($"page {page.Title} is protected by roles that user is not in so redirecting");
                    return RedirectToRoute(pageRoutes.PageRouteName);
                }
            }

            var resource = new PageResource();
            resource.ContentId = page.Id;
            resource.Type = model.Type;
            resource.Environment = model.Environment;
            resource.Sort = model.Sort;
            resource.Url = model.Url;
            page.Resources.Add(resource);
            


            await pageService.Update(page, page.IsPublished);

            return RedirectToRoute(pageRoutes.PageDevelopRouteName, new { slug = page.Slug });

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveResource(string slug, string id)
        {
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("redirecting to index because project settings not found");

                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);
            if (!canEdit)
            {
                log.LogInformation("redirecting to index because user is not allowed to edit");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }
            var canDev = editOptions.AlwaysShowDeveloperLink ? true : User.IsInRole(editOptions.DeveloperAllowedRole);
            if (!canDev)
            {
                log.LogInformation("redirecting to index because user is not allowed by edit config for developer tools");
                return RedirectToRoute(pageRoutes.PageRouteName);
            }

            IPage page = null;
            if (!string.IsNullOrEmpty(slug))
            {
                page = await pageService.GetPageBySlug(slug);
            }

            if (page == null)
            {
                log.LogInformation("page not found, redirecting");
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            if ((!string.IsNullOrEmpty(page.ViewRoles)))
            {
                if (!User.IsInRoles(page.ViewRoles))
                {
                    log.LogWarning($"page {page.Title} is protected by roles that user is not in so redirecting");
                    return RedirectToRoute(pageRoutes.PageRouteName);
                }
            }

            var found = false;
            var copyOfResources = page.Resources.ToList();
            for (var i = 0; i < copyOfResources.Count; i++)
            {
                if(copyOfResources[i].Id == id)
                {
                    found = true;
                    copyOfResources.RemoveAt(i);
                }
            }
            if(found)
            {
                // needed so the ef entity shadow copy of resources gets synced
                page.Resources = copyOfResources;
                await pageService.Update(page, page.IsPublished);
            }
            else
            {
                log.LogWarning($"page resource not found for {slug} and {id}");
            }
            

            return RedirectToRoute(pageRoutes.PageDevelopRouteName, new { slug = page.Slug });

        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                
                log.LogInformation("project not found, redirecting/rejecting");
                if (Request.IsAjaxRequest())
                {
                    return BadRequest();
                }
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug="" });
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("user is not allowed to edit, redirecting/rejecting");
                if (Request.IsAjaxRequest())
                {
                    return BadRequest();
                }
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            if (string.IsNullOrEmpty(id))
            {
                log.LogInformation("postid not provided, redirecting/rejecting");
                if (Request.IsAjaxRequest())
                {
                    return BadRequest();
                }
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });

            }

            var page = await pageService.GetPage(id);

            if (page == null)
            {
                log.LogInformation($"page not found for {id}, redirecting/rejecting");
                if (Request.IsAjaxRequest())
                {
                    return BadRequest();
                }
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            if ((!string.IsNullOrEmpty(page.ViewRoles)))
            {
                if (!User.IsInRoles(page.ViewRoles))
                {
                    log.LogWarning($"page {page.Title} is protected by roles that user is not in so redirecting");
                    return RedirectToRoute(pageRoutes.PageRouteName);
                }
            }

            if (page.Slug == project.DefaultPageSlug) // don't allow delete the home/default page from the ui
            {
                log.LogWarning($"Rejecting/redirecting, user {User.Identity.Name} tried to delete the default page {page.Slug}");
                if (Request.IsAjaxRequest())
                {
                    return BadRequest();
                }
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            

            await pageService.DeletePage(page.Id);
            pageService.ClearNavigationCache();

            log.LogWarning($"user {User.Identity.Name} deleted page {page.Slug}");

            if (Request.IsAjaxRequest())
            {
                return Json(new PageActionResult(true,"success"));
            }

            return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });

        }

        [HttpGet]
        public IActionResult SiteMap()
        {
            return View("SiteMapIndex");
        }

        [HttpGet]
  
        public async Task<IActionResult> Tree()
        {
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("project not found, redirecting");
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("user is not allowed to edit, redirecting");
                return RedirectToRoute(pageRoutes.PageRouteName, new { slug = "" });
            }

            ViewData["Title"] = sr["Page Management"];
            var model = new PageTreeViewModel();
            model.TreeServiceUrl = Url.Action("TreeJson");
            model.EditUrl = Url.RouteUrl(pageRoutes.PageEditRouteName);
            model.ViewUrl = Url.RouteUrl(pageRoutes.PageRouteName);
            model.MoveUrl = Url.Action("Move");
            model.DeleteUrl = Url.Action("Delete");
            model.SortUrl = Url.Action("SortChildPagesAlpha");

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TreeJson(string node = "root")
        {
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("project not found");
                return BadRequest();
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("user is not allowed to edit");
                return BadRequest();
            }

            var result = await pageService.GetPageTreeJson(User, node);

            return new ContentResult
            {
                ContentType = "application/json",
                Content = result,
                StatusCode = 200
            };

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Move(PageMoveModel model)
        {
            if (model == null)
            {
                log.LogInformation("model was null");
                return BadRequest();
            }

            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("project not found");
                return BadRequest();
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("user is not allowed to edit");
                return BadRequest();
            }


            var result = await pageService.Move(model);

            return Json(result);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SortChildPagesAlpha(string pageId)
        {
            var project = await projectService.GetCurrentProjectSettings();

            if (project == null)
            {
                log.LogInformation("project not found");
                return BadRequest();
            }

            var canEdit = await User.CanEditPages(project.Id, authorizationService);

            if (!canEdit)
            {
                log.LogInformation("user is not allowed to edit");
                return BadRequest();
            }


            var result = await pageService.SortChildPagesAlpha(pageId);

            return Json(result);
        }

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AjaxPost(PageEditViewModel model)
        //{
        //    // disable status code page for ajax requests
        //    var statusCodePagesFeature = HttpContext.Features.Get<IStatusCodePagesFeature>();
        //    if (statusCodePagesFeature != null)
        //    {
        //        statusCodePagesFeature.Enabled = false;
        //    }

        //    if (string.IsNullOrEmpty(model.Title))
        //    {
        //        // if a page has been configured to not show the title
        //        // this may be null on edit, if it is a new page then it should be required
        //        // because it is used for generating the slug
        //        //if (string.IsNullOrEmpty(model.Slug))
        //        //{
        //        log.LogInformation("returning 500 because no title was posted");
        //        return StatusCode(500);
        //        //}

        //    }

        //    var project = await projectService.GetCurrentProjectSettings();

        //    if (project == null)
        //    {
        //        log.LogInformation("returning 500 blog not found");
        //        return StatusCode(500);
        //    }

        //    var canEdit = await User.CanEditPages(project.Id, authorizationService);

        //    if (!canEdit)
        //    {
        //        log.LogInformation("returning 403 user is not allowed to edit");
        //        return StatusCode(403);
        //    }

        //    //string[] categories = new string[0];
        //    //if (!string.IsNullOrEmpty(model.Categories))
        //    //{
        //    //    categories = model.Categories.Split(new char[] { ',' },
        //    //    StringSplitOptions.RemoveEmptyEntries);
        //    //}


        //    IPage page = null;
        //    if (!string.IsNullOrEmpty(model.Id))
        //    {
        //        page = await pageService.GetPage(model.Id);
        //    }

        //    var needToClearCache = false;
        //    var isNew = false;
        //    if (page != null)
        //    {
        //        if(page.Title != model.Title)
        //        {
        //            needToClearCache = true;
        //        }
        //        page.Title = model.Title;
        //        page.MetaDescription = model.MetaDescription;
        //        page.Content = model.Content;
        //        if (page.PageOrder != model.PageOrder) needToClearCache = true;

        //    }
        //    else
        //    {
        //        isNew = true;
        //        needToClearCache = true;
        //        var slug = ContentUtils.CreateSlug(model.Title);
        //        var available = await pageService.SlugIsAvailable(project.Id, slug);
        //        if (!available)
        //        {
        //            log.LogInformation("returning 409 because slug already in use");
        //            return StatusCode(409);
        //        }

        //        page = new Page()
        //        {
        //            ProjectId = project.Id,
        //            Author = User.GetUserDisplayName(),
        //            Title = model.Title,
        //            MetaDescription = model.MetaDescription,
        //            Content = model.Content,
        //            Slug = slug,
        //            ParentId = "0"

        //            //,Categories = categories.ToList()
        //        };
        //    }

        //    if(!string.IsNullOrEmpty(model.ParentSlug))
        //    {
        //        var parentPage = await pageService.GetPageBySlug(project.Id, model.ParentSlug);
        //        if (parentPage != null)
        //        {
        //            if(parentPage.Id != page.ParentId)
        //            {
        //                page.ParentId = parentPage.Id;
        //                page.ParentSlug = parentPage.Slug;
        //                needToClearCache = true;
        //            }

        //        }
        //    }
        //    else
        //    {
        //        // empty means root level
        //        page.ParentSlug = string.Empty;
        //        page.ParentId = "0";
        //    }
        //    if(page.ViewRoles != model.ViewRoles)
        //    {
        //        needToClearCache = true;
        //    }
        //    page.ViewRoles = model.ViewRoles;

        //    page.PageOrder = model.PageOrder;
        //    page.IsPublished = model.IsPublished;
        //    page.ShowHeading = model.ShowHeading;
        //    page.MenuOnly = model.MenuOnly;
        //    if (!string.IsNullOrEmpty(model.PubDate))
        //    {
        //        var localTime = DateTime.Parse(model.PubDate);
        //        page.PubDate = timeZoneHelper.ConvertToUtc(localTime, project.TimeZoneId);

        //    }

        //    if(isNew)
        //    {
        //        await pageService.Create(page, model.IsPublished);
        //    }
        //    else
        //    {
        //        await pageService.Update(page, model.IsPublished);
        //    }


        //    if(needToClearCache)
        //    {
        //        pageService.ClearNavigationCache();
        //    }

        //    var url = Url.RouteUrl(pageRoutes.PageRouteName, new { slug = page.Slug });
        //    return Content(url);

        //}



        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> AjaxDelete(string id)
        //{
        //    // disable status code page for ajax requests
        //    var statusCodePagesFeature = HttpContext.Features.Get<IStatusCodePagesFeature>();
        //    if (statusCodePagesFeature != null)
        //    {
        //        statusCodePagesFeature.Enabled = false;
        //    }

        //    var project = await projectService.GetCurrentProjectSettings();

        //    if (project == null)
        //    {
        //        log.LogInformation("returning 500 blog not found");
        //        return StatusCode(500);
        //    }

        //    var canEdit = await User.CanEditPages(project.Id, authorizationService);

        //    if (!canEdit)
        //    {
        //        log.LogInformation("returning 403 user is not allowed to edit");
        //        return StatusCode(403);
        //    }

        //    if (string.IsNullOrEmpty(id))
        //    {
        //        log.LogInformation("returning 404 postid not provided");
        //        return StatusCode(404);
        //    }

        //    var page = await pageService.GetPage(id);

        //    if (page == null)
        //    {
        //        log.LogInformation("returning 404 not found");
        //        return StatusCode(404);
        //    }

        //    log.LogWarning("user " + User.Identity.Name + " deleted page " + page.Slug);

        //    await pageService.DeletePage(project.Id, page.Id);

        //    return StatusCode(200);

        //}


    }
}
