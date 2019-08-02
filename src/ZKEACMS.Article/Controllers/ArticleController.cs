/*!
 * http://www.zkea.net/
 * Copyright 2017 ZKEASOFT
 * http://www.zkea.net/licenses
 */

using Easy.Constant;
using Easy.Mvc.Authorize;
using Easy.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using ZKEACMS.Article.ActionFilter;
using ZKEACMS.Article.Models;
using ZKEACMS.Article.Service;
using Easy.Extend;
using Easy;
using System.Linq;
using System.Collections.Generic;
using Easy.Mvc;

namespace ZKEACMS.Article.Controllers
{
    [DefaultAuthorize, ViewDataArticleType]
    public class ArticleController : BasicController<ArticleEntity, int, IArticleService>
    {
        private readonly IAuthorizer _authorizer;
        private readonly IApplicationContext _applicationContext;
        public ArticleController(IArticleService service, IAuthorizer authorizer, IApplicationContext applicationContext)
            : base(service)
        {
            _authorizer = authorizer;
            _applicationContext = applicationContext;
        }
        [DefaultAuthorize(Policy = PermissionKeys.ViewArticle)]
        public override IActionResult Index()
        {
            return base.Index();
        }
        [DefaultAuthorize(Policy = PermissionKeys.ManageArticle)]
        public override IActionResult Create()
        {
            return base.Create();
        }
        [HttpPost, DefaultAuthorize(Policy = PermissionKeys.ManageArticle)]
        public override IActionResult Create(ArticleEntity entity)
        {
            var result = base.Create(entity);
            if (entity.ActionType == ActionType.Publish && _authorizer.Authorize(PermissionKeys.PublishArticle))
            {
                Service.Publish(entity.ID);
            }
            return result;
        }
        [DefaultAuthorize(Policy = PermissionKeys.ManageArticle)]
        public override IActionResult Edit(int Id)
        {
            var entity = Service.Get(Id);
            if (entity != null && entity.CreateBy != _applicationContext.CurrentUser.UserID && !_authorizer.Authorize(PermissionKeys.ManageAllArticle))
            {
                return RedirectToAction("Index");
            }
            return View(entity);
        }
        [HttpPost, DefaultAuthorize(Policy = PermissionKeys.ManageArticle)]
        public override IActionResult Edit(ArticleEntity entity)
        {
            if ((entity.IsPublish && _authorizer.Authorize(PermissionKeys.EditPublishedArticle)) || !entity.IsPublish)
            {
                var result = base.Edit(entity);
                if (entity.ActionType == ActionType.Publish && _authorizer.Authorize(PermissionKeys.PublishArticle))
                {
                    Service.Publish(entity.ID);
                    if (Request.Query["ReturnUrl"].Count > 0)
                    {
                        return Redirect(Request.Query["ReturnUrl"]);
                    }
                }
                return result;
            }
            return RedirectToAction("index");
        }
        [HttpPost, DefaultAuthorize(Policy = PermissionKeys.ViewArticle)]
        public override IActionResult GetList(DataTableOption query)
        {
            if (!_authorizer.Authorize(PermissionKeys.ManageAllArticle))
            {
                var create = query.Columns.FirstOrDefault(m => m.Data == "CreateBy");
                if (create != null)
                {
                    create.Search.Value = _applicationContext.CurrentUser.UserID;
                    create.Search.Opeartor = Easy.LINQ.Query.Operators.Equal;
                }
                else
                {
                    var column = new ColumnOption();
                    column.Data = "CreateBy";
                    column.Search.Value = _applicationContext.CurrentUser.UserID;
                    column.Search.Opeartor = Easy.LINQ.Query.Operators.Equal;
                    query.Columns = query.Columns.Concat(new List<ColumnOption> { column }).ToArray();
                }
            }
            return base.GetList(query);
        }
        [DefaultAuthorize(Policy = PermissionKeys.ManageArticle)]
        public override IActionResult Delete(int id)
        {
            var entity = Service.Get(id);
            if ((entity.IsPublish && _authorizer.Authorize(PermissionKeys.ManageAllArticle)) || !entity.IsPublish)
            {
                return base.Delete(id);
            }
            return Json(new AjaxResult { Status = AjaxStatus.Normal });
        }
    }
}

