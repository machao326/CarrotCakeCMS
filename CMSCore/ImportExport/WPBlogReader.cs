﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
/*
* CarrotCake CMS
* http://www.carrotware.com/
*
* Copyright 2011, Samantha Copeland
* Dual licensed under the MIT or GPL Version 2 licenses.
*
* Date: October 2011
*/


namespace Carrotware.CMS.Core {

	public class WPBlogReader {
		public WPBlogReader() { }

		public XmlDocument LoadFile(string FileName) {
			XmlDocument doc = new XmlDocument();
			doc.Load(FileName);
			return doc;
		}

		public XmlDocument LoadText(string InputString) {
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(InputString);
			return doc;
		}


		public WordPressSite ParseDoc(XmlDocument doc) {
			WordPressSite site = new WordPressSite();

			List<WordPressPost> lstWPP = new List<WordPressPost>();

			XmlNode rssNode = doc.SelectSingleNode("//rss");

			XmlNamespaceManager rssNamespace = new XmlNamespaceManager(doc.NameTable);

			foreach (XmlAttribute attrib in rssNode.Attributes) {
				if (attrib != null && attrib.Value.ToLower().StartsWith("http")) {
					rssNamespace.AddNamespace(attrib.LocalName, attrib.Value);
				}
			}

			site.SiteTitle = rssNode.SelectSingleNode("channel/title").InnerText;
			site.SiteDescription = rssNode.SelectSingleNode("channel/description").InnerText;
			site.SiteURL = rssNode.SelectSingleNode("channel/link").InnerText;
			site.ImportSource = rssNode.SelectSingleNode("channel/generator").InnerText;
			site.ExtractDate = Convert.ToDateTime(rssNode.SelectSingleNode("channel/pubDate").InnerText);
			site.wxrVersion = rssNode.SelectSingleNode("channel/wp:wxr_version", rssNamespace).InnerText;

			site.Categories = new List<InfoKVP>();

			XmlNodeList catNodes = doc.SelectNodes("//rss/channel/wp:category", rssNamespace);
			foreach (XmlNode node in catNodes) {
				string slug = node.SelectSingleNode("wp:category_nicename", rssNamespace).InnerText;
				string title = node.SelectSingleNode("wp:cat_name", rssNamespace).InnerText;
				site.Categories.Add(new InfoKVP(slug, title));
			}
			catNodes = doc.SelectNodes("//rss/channel/item/category[@domain='category']");
			foreach (XmlNode node in catNodes) {
				if (node.Attributes["nicename"] != null) {
					string slug = node.Attributes["nicename"].InnerText;
					string title = node.InnerText;
					site.Categories.Add(new InfoKVP(slug, title));
				}
			}


			site.Tags = new List<InfoKVP>();

			XmlNodeList tagNodes = doc.SelectNodes("//rss/channel/wp:tag", rssNamespace);
			foreach (XmlNode node in tagNodes) {
				string slug = node.SelectSingleNode("wp:tag_slug", rssNamespace).InnerText;
				string title = node.SelectSingleNode("wp:tag_name", rssNamespace).InnerText;
				site.Tags.Add(new InfoKVP(slug, title));
			}
			tagNodes = doc.SelectNodes("//rss/channel/item/category[@domain='post_tag']");
			foreach (XmlNode node in tagNodes) {
				if (node.Attributes["nicename"] != null) {
					string slug = node.Attributes["nicename"].InnerText;
					string title = node.InnerText;
					site.Tags.Add(new InfoKVP(slug, title));
				}
			}

			XmlNodeList nodes = doc.SelectNodes("//rss/channel/item");

			foreach (XmlNode node in nodes) {
				WordPressPost wpp = new WordPressPost();
				wpp.PostType = WordPressPost.WPPostType.Unknown;
				wpp.IsPublished = false;
				wpp.PostOrder = 0;
				wpp.ImportRootID = Guid.NewGuid();

				wpp.PostTitle = node.SelectSingleNode("title").InnerText.Trim();
				wpp.PostName = node.SelectSingleNode("wp:post_name", rssNamespace).InnerText.Trim();

				if (string.IsNullOrEmpty(wpp.PostName)) {
					wpp.PostName = wpp.PostTitle.ToLower();
				}
				if (string.IsNullOrEmpty(wpp.PostName)) {
					wpp.PostName = wpp.ImportRootID.ToString().ToLower();
				}
				if (string.IsNullOrEmpty(wpp.PostTitle)) {
					wpp.PostTitle = "(No Title)";
				}

				wpp.PostName = ContentPageHelper.ScrubSlug(wpp.PostName);

				wpp.PostDate = Convert.ToDateTime(node.SelectSingleNode("wp:post_date", rssNamespace).InnerText);
				wpp.PostContent = node.SelectSingleNode("content:encoded", rssNamespace).InnerText;

				if (string.IsNullOrEmpty(wpp.PostContent)) {
					wpp.PostContent = "";
				}
				wpp.PostContent = wpp.PostContent.Replace("\r\n", "\n").Trim();

				wpp.ParentPostID = int.Parse(node.SelectSingleNode("wp:post_parent", rssNamespace).InnerText);
				wpp.PostID = int.Parse(node.SelectSingleNode("wp:post_id", rssNamespace).InnerText);
				wpp.PostOrder = int.Parse(node.SelectSingleNode("wp:menu_order", rssNamespace).InnerText);

				if (node.SelectSingleNode("wp:status", rssNamespace).InnerText == "publish") {
					wpp.IsPublished = true;
				}

				string postType = node.SelectSingleNode("wp:post_type", rssNamespace).InnerText;

				switch (postType) {
					case "attachment":
						wpp.PostType = WordPressPost.WPPostType.Attachment;
						break;
					case "post":
						wpp.PostType = WordPressPost.WPPostType.BlogPost;
						break;
					case "page":
						wpp.PostType = WordPressPost.WPPostType.Page;
						break;
				}

				if (wpp.PostType == WordPressPost.WPPostType.BlogPost
					|| (wpp.PostType == WordPressPost.WPPostType.Page && wpp.ParentPostID > 0)) {
					wpp.PostOrder = wpp.PostOrder + 10;
				}


				wpp.Categories = new List<string>();
				XmlNodeList nodesCat = node.SelectNodes("category[@domain='category']");
				foreach (XmlNode n in nodesCat) {
					if (n.Attributes["nicename"] != null) {
						wpp.Categories.Add(n.Attributes["nicename"].Value);
					}
				}

				wpp.Tags = new List<string>();
				XmlNodeList nodesTag = node.SelectNodes("category[@domain='post_tag']");
				foreach (XmlNode n in nodesTag) {
					if (n.Attributes["nicename"] != null) {
						wpp.Tags.Add(n.Attributes["nicename"].Value);
					}
				}

				wpp.PostContent = wpp.PostContent.Replace('\u00A0', ' ').Replace("\n\n\n", "\n\n").Replace("\n\n\n", "\n\n");

				wpp.PostContent = "<p>" + wpp.PostContent.Replace("\n\n", "</p><p>") + "</p>";
				wpp.PostContent = wpp.PostContent.Replace("\n", "<br />\n");
				wpp.PostContent = wpp.PostContent.Replace("</p><p>", "</p>\n<p>");

				wpp.ImportFileSlug = ContentPageHelper.ScrubFilename(wpp.ImportRootID, "/" + wpp.PostName.Trim() + ".aspx");
				wpp.ImportFileName = ContentPageHelper.ScrubFilename(wpp.ImportRootID, wpp.ImportFileSlug);

				lstWPP.Add(wpp);
			}

			foreach (WordPressPost w in lstWPP.Where(x => x.ParentPostID > 0 && x.PostType == WordPressPost.WPPostType.Page)) {
				if (lstWPP.Where(x => x.PostID == w.ParentPostID
							&& x.PostType == WordPressPost.WPPostType.Page).Count() > 0) {
					WordPressPost p = lstWPP.Where(x => x.PostID == w.ParentPostID).FirstOrDefault();
					w.ImportParentRootID = p.ImportRootID;
					w.ImportFileName = "/" + p.PostName.Trim() + w.ImportFileSlug;
				}
			}

			lstWPP.RemoveAll(x => x.PostType == WordPressPost.WPPostType.Attachment);

			site.Content = lstWPP;

			return site;
		}

	}
}
