﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deployer.Repo;

namespace Deployer
{
	public class Context : IDisposable
	{
		public DBase dBase = new DBase();

        /// <summary>
        /// All existing modules
        /// </summary>
        public BindingList<string> Modules = new BindingList<string>();

        /// <summary>
        /// Releases for Current Module
        /// </summary>
        public BindingList<string> Releases = new BindingList<string>();

        /// <summary>
        /// Externals for current release of current module
        /// </summary>
        public BindingList<string> ReleaseExternals = new BindingList<string>();


        public int ModuleIndex;

        /// <summary>
        /// Currently selected dmodule
        /// </summary>
        public string Module
        {
            get{
                if( ModuleIndex < 0 || ModuleIndex >= Modules.Count )
                    return String.Empty;
                return Modules[ModuleIndex];
            }
            set{

                ModuleIndex = Modules.IndexOf( value );
            }
        }

        public int ReleaseIndex;

        /// <summary>
        /// Currently selected release
        /// </summary>
        public string Release
        {
            get{
                if( ReleaseIndex < 0 ||ReleaseIndex >= Releases.Count )
                    return String.Empty;
                return Releases[ReleaseIndex];
            }
            set{

                ReleaseIndex = Releases.IndexOf( value );
            }
        }

        /// <summary>
        /// Install site list
        /// </summary>
        public BindingList<string> Installs = new BindingList<string>();


        public int InstallIndex;

        /// <summary>
        /// Currently selected Install
        /// </summary>
        public string Install
        {
            get{
                if( InstallIndex < 0 || InstallIndex >= Installs.Count )
                    return String.Empty;
                return Installs[InstallIndex];
            }
            set{

                InstallIndex = Installs.IndexOf( value );
            }
        }


        /// <summary>
        /// Externals for current install
        /// </summary>
        public BindingList<string> InstallExternals = new BindingList<string>();


        public void Dispose()
        {
            //frmRel.Dispose();
            //frmRel = null;
        }

        // singleton pattern
		private static Context instance=null;

        private Context()
        {
        }

        public static Context Instance
        {
            get
            {
                if (instance==null)
                {
                    instance = new Context();
                }
                return instance;
            }
        }

		 
         /// <summary>
         /// url of given release for currently selected module
         /// </summary>
		public string GetReleaseUrl( string relName )
		{
			string releaseBaseUrl = dBase.GetReleaseModuleUrl( Module ); 
			return $"{releaseBaseUrl}/{relName}";
		}

        /// <summary>
        /// url of given Install
        /// </summary>
		public string GetInstallUrl( string installName )
		{
			string installBaseUrl = dBase.GetInstallRootUrl(); 
			return $"{installBaseUrl}/{installName}/trunk";
		}


        public bool ScanRepo()
        {
            if( !dBase.IsRepoValid )
                return false;

            ReloadModules();
            ReloadReleases();
            ReloadReleaseExternals();
            ReloadInstalls();
            ReloadInstallExternals();

            return true;
        }

        public void ReloadModules( string valueToSelect=null )
        {
            if( String.IsNullOrEmpty(valueToSelect) )
                valueToSelect = Module;

            List<string> modules;
            RepoScanner.ScanModules( dBase.svnClient, dBase.GetReleaseRootUrl(), out modules );
            Modules.Clear();        
            foreach( var i in modules ) Modules.Add(i);

            // if current module still exists, pick it
            if( !String.IsNullOrEmpty( valueToSelect ) && Modules.Contains( valueToSelect ) )
            {
                ModuleIndex = Modules.IndexOf( valueToSelect );
            }
            else if( Modules.Count > 0 )
            {
                ModuleIndex = 0;    
            }
            else
            {
                ModuleIndex = -1;
            }
        }

        public void ReloadReleases( string valueToSelect=null )
        {
            if( String.IsNullOrEmpty(valueToSelect) )
                valueToSelect = Release;

            if( !String.IsNullOrEmpty( Module ) )
            {
                List<string> releases;
                RepoScanner.ScanReleases(
                    dBase.svnClient,
                    dBase.GetReleaseModuleUrl(Module),
                    out releases
                   );
                Releases.Clear();
                foreach( var i in releases ) Releases.Add(i);
            }
            else
            {
                Releases.Clear();
            }


            // if current release still exist in the list, reload its releases
            if( !String.IsNullOrEmpty( valueToSelect ) && Releases.Contains( valueToSelect ) )
            {
                ReleaseIndex = Releases.IndexOf( valueToSelect );
            }
            else if( Releases.Count > 0 )
            {
                ReleaseIndex = 0;    
            }
            else
            {
                ReleaseIndex = -1;
            }
        }

        public void ReloadInstalls( string valueToSelect=null )
        {
            if( String.IsNullOrEmpty(valueToSelect) )
                valueToSelect = Install;


            List<string> installs;
            RepoScanner.ScanInstalls(
                dBase.svnClient,
                dBase.GetInstallRootUrl(),
                out installs
            );

            Installs.Clear();
            foreach( var i in installs ) Installs.Add(i);

            // if current module still exists, pick it
            if( !String.IsNullOrEmpty( valueToSelect ) && Installs.Contains( valueToSelect ) )
            {
                InstallIndex = Installs.IndexOf( valueToSelect );
            }
            else if( Installs.Count > 0 )
            {
                InstallIndex = 0;    
            }
            else
            {
                InstallIndex = -1;
            }
        }

        public void ReloadReleaseExternals()
        {
            ReleaseExternals.Clear();
            if( !String.IsNullOrEmpty( Release ) )
            {
                SharpSvn.SvnExternalItem[] extItems;
                Exter.ReadExternals(
                    dBase.svnClient, 
                    GetReleaseUrl( Release ),
                    out extItems
                );

                foreach( var i in extItems )
                {
                    var r = i.Reference;
                    
                    // strip the unimportant beginning of the link, show just the link/branch type and revision number
                    //var removableStart = $"^/{dBase.ShrSegm}/{Module}/{i.Target}/";
                    var removableStart = Exter.StripStdSvnLayoutFromUrl( i.Reference )+"/";
                    if( r.StartsWith( removableStart ) )
                    {
                        r = r.Substring( removableStart.Length );
                    }

                    
                    var s = "";
                    if( i.Revision.RevisionType == SharpSvn.SvnRevisionType.Number )
                    {
                        s = $"{i.Target} => {r}@{i.Revision.Revision}";
                    }
                    else
                    {
                        s = $"{i.Target} => {r}";
                    }

                    ReleaseExternals.Add( s );
                }
            }

        }


		public void ReloadInstallExternals()
		{
			InstallExternals.Clear();
			if (!String.IsNullOrEmpty(Release))
			{
				SharpSvn.SvnExternalItem[] extItems;
				Exter.ReadExternals(
					dBase.svnClient,
					GetInstallUrl(Install),
					out extItems
				);

				foreach (var i in extItems)
				{
                    var r = i.Reference;
                    
                    // strip the unimportant beginning of the reference, show just the linked release name
                    var removableStart = $"^/{dBase.RelSegm}/{i.Target}/";
                    if( r.StartsWith( removableStart ) )
                    {
                        r = r.Substring( removableStart.Length );
                    }

                    var s = "";
                    if( i.Revision.RevisionType == SharpSvn.SvnRevisionType.Number )
                    {
                        s = $"{i.Target} => {r}@{i.Revision.Revision}";
                    }
                    else
                    {
                        s = $"{i.Target} => {r}";
                    }

					InstallExternals.Add(s);
				}
			}

		}



	}
}                               
