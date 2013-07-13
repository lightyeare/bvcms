﻿using System;
using System.Collections.Generic;
using System.Linq;
using CmsData;
using CmsWeb.Areas.Finance.Controllers;
using CmsWeb.Models;
using UtilityExtensions;
using CmsData.Codes;

namespace CmsWeb.Areas.Finance.Models
{
    public class BundlesModel : PagerModel2
    {
        int? _count;
        public int Count()
        {
            if (!_count.HasValue)
                _count = FetchBundles().Count();
            return _count.Value;
        }

        public BundlesModel()
        {
            GetCount = Count;
            Sort = "Status";
        }
        private IQueryable<CmsData.View.BundleList> _bundles;

        private IQueryable<CmsData.View.BundleList> FetchBundles()
        {
            if (_bundles == null)
                _bundles = from b in DbUtil.Db.ViewBundleLists select b;
            return _bundles;
        }

        public IEnumerable<BundleInfo> Bundles()
        {
            var q = ApplySort();
            var q2 = q.Skip(StartRow).Take(PageSize);
            var q3 = from b in q2
                     select new BundleInfo
                                {
                                    BundleId = b.BundleHeaderId,
                                    HeaderType = b.HeaderType,
                                    PostingDate = b.PostingDate,
                                    DepositDate = b.DepositDate,
                                    TotalBundle = b.TotalBundle,
                                    TotalItems = b.TotalItems,
                                    ItemCount = b.ItemCount ?? 0,
                                    TotalNonTaxDed = b.TotalNonTaxDed,
                                    FundId = b.FundId,
                                    Fund = b.Fund,
                                    Status = b.Status,
                                    open = b.Open == 1 
                                };
            return q3;
        }
        public IQueryable<CmsData.View.BundleList> ApplySort()
        {
            var q = FetchBundles();
            if (Direction == "asc")
                switch (Sort)
                {
                    case "Type":
                        q = from b in q
                            orderby b.HeaderType
                            select b;
                        break;
                    case "Deposited":
                        q = from b in q
                            orderby b.DepositDate
                            select b;
                        break;
                    case "Total Bundle":
                        q = from b in q
                            orderby b.TotalBundle
                            select b;
                        break;
                    case "Items":
                        q = from b in q
                            orderby b.TotalItems
                            select b;
                        break;
                    case "Count":
                        q = from b in q
                            orderby b.ItemCount
                            select b;
                        break;
                    case "Posted":
                        q = from b in q
                            orderby b.PostingDate
                            select b;
                        break;
                    case "Id":
                    case "Status":
                        q = from b in q
                            orderby b.Status descending, b.BundleHeaderId descending 
                            select b;
                        break;
                }
            else
                switch (Sort)
                {
                    case "Type":
                        q = from b in q
                            orderby b.HeaderType descending 
                            select b;
                        break;
                    case "Deposited":
                        q = from b in q
                            orderby b.DepositDate descending 
                            select b;
                        break;
                    case "Total Bundle":
                        q = from b in q
                            orderby b.TotalBundle descending 
                            select b;
                        break;
                    case "Items":
                        q = from b in q
                            orderby b.TotalItems descending 
                            select b;
                        break;
                    case "Count":
                        q = from b in q
                            orderby b.ItemCount descending 
                            select b;
                        break;
                    case "Posted":
                        q = from b in q
                            orderby b.PostingDate descending 
                            select b;
                        break;
                    case "Id":
                    case "Status":
                        q = from b in q
                            orderby b.Status descending, b.BundleHeaderId descending 
                            select b;
                        break;
                }
            return q;
        }
    }
    public class BundleInfo
    {
        public int BundleId { get; set; }
        public DateTime? PostingDate { get; set; }
        public string HeaderType { get; set; }
        public DateTime? DepositDate { get; set; }
        public decimal? TotalBundle { get; set; }
        public decimal? TotalItems { get; set; }
        public decimal? TotalNonTaxDed { get; set; }
        public int ItemCount { get; set; }
        public int? FundId { get; set; }
        public string Fund { get; set; }
        public string Status { get; set; }
        public bool open { get; set; }
    }
}