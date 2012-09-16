using System;
using System.Web;
// This is needed for the DependencyResolver...wish they would've just used Common Service Locator!
using Vaiona.Persistence.Api;

namespace Vaiona.Web.HttpModules
{
    /// <summary>
    /// Taken from http://nhforge.org/blogs/nhibernate/archive/2011/03/03/effective-nhibernate-session-management-for-web-apps.aspx
    /// </summary>
    public class SessionPerRequestModule : IHttpModule
    {
        IPersistenceManager pManager = null;
        public void Init(HttpApplication context)
        {
            context.BeginRequest += ContextBeginRequest;
            context.EndRequest += ContextEndRequest;
            context.Error += ContextError;
            pManager = PersistenceFactory.GetPersistenceManager();
        }

        private void ContextBeginRequest(object sender, EventArgs e)
        {
            pManager.StartConversation();
        }

        private void ContextEndRequest(object sender, EventArgs e)
        {
            pManager.EndConversation();
        }

        private void ContextError(object sender, EventArgs e)
        {
            pManager.EndContext();
        }

        public void Dispose() { }
    }
}