﻿//-----------------------------------------------------------------------
// <copyright file="BundleConfig.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2015 Dave Mateer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Otter
{
    using System.Web.Optimization;

    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            IItemTransform transform = new CssRewriteUrlTransformWrapper();
            bundles.Add(new ScriptBundle("~/bundles/scripts/common").Include("~/Scripts/jquery-{version}.js", "~/Scripts/bootstrap.js", "~/Scripts/jquery-ui.js", "~/Scripts/jquery.validate.js", "~/Scripts/jquery.validate.unobtrusive.js", "~/Scripts/main.js"));
            bundles.Add(new StyleBundle("~/bundles/css/common")
                .Include("~/Content/jquery-ui.css", transform)
                .Include("~/Content/bootstrap.css", transform)
                .Include("~/Content/bootstrap-theme.css", transform)
                .Include("~/Content/otter.css", transform));
            BundleTable.EnableOptimizations = true;
        }
    }
}