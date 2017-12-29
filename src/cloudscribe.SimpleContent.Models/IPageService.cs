﻿using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;


namespace cloudscribe.SimpleContent.Models
{
    public interface IPageService
    {
        Task DeletePage(string pageId);
        Task<List<IPage>> GetAllPages(
            string projectId,
            string userName,
            string password);
        Task<List<IPage>> GetRootPages();
        Task<List<IPage>> GetChildPages(string pageId);
        Task<IPage> GetPage(string projectId, string pageId, string userName, string password);
        Task<IPage> GetPage(string pageId);

        Task<IPage> GetPageBySlug(string slug);
        //Task<bool> PageSlugIsAvailable(string slug);
        //Task<bool> PageSlugIsAvailable(string projectId, string slug);

        Task<bool> SlugIsAvailable(string slug);
        Task<string> ResolvePageUrl(IPage page);

        Task Create(
            IPage page,
            bool publish);

        Task Update(
            IPage page,
            bool publish);

        Task Create(
            string projectId, 
            string userName,
            string password,
            IPage page, 
            bool publish);

        Task Update(
            string projectId,
            string userName,
            string password,
            IPage page,
            bool publish);

       

        void ClearNavigationCache();

        Task<string> GetPageTreeJson(ClaimsPrincipal user, string node = "root");

        Task<PageActionResult> Move(PageMoveModel model);

        Task<PageActionResult> SortChildPagesAlpha(string pageId);

        Task<int> GetNextChildPageOrder(string pageId);
    }
}