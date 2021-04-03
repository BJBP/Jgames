using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace BuildHelper.Editor.Core {
    /// <summary>
    /// Provides request to external git program.
    /// Before using this, make sure you have git installed and your project has a git repository.
    /// By default it is considered that git is declared in the <i>PATH</i> environment variable.
    /// To specify the path to git, change <see cref="BuildHelperStrings.GIT_EXEC_PATH"/>
    /// </summary>
    internal static class GitRequest {
        /// <summary>
        /// </summary>
        /// <returns>Current branch name for project</returns>
        /// <exception cref="ExternalException">Throws if git request failed</exception>
        public static string CurrentBranch() {
            return CreateRequestGit(
                "rev-parse --abbrev-ref HEAD"
            ).Execute().Trim();
        }

        /// <summary>
        /// Generate revision string for current git repository state.
        /// There is two type of returns:
        /// <list type="bullet">
        ///     <item>
        ///         <description>Number for pretty version, e.g. "234". It indicates count of commits in current branch</description>
        ///     </item>
        ///     <item>
        ///         <description>Short revision id, e.g. "1685c5a". It indicates short id of <i>HEAD</i></description>
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="getNumber">If <i>true</i>, returns number, otherwise short revision id</param>
        /// <returns>String</returns>
        /// <exception cref="ExternalException">Throws if git request failed</exception>
        /// <seealso cref="FindRevision"/>
        public static string Revision(bool getNumber) {
            return CreateRequestGit(getNumber ? 
                "rev-list --count HEAD" :
                "rev-parse --short HEAD"
            ).Execute().Trim();
        }

        /// <summary>
        /// Checkout specified branch and refresh <i>AssetDatabase</i>
        /// </summary>
        /// <param name="branch">branch to checkout</param>
        /// <exception cref="ExternalException">Throws if git request failed</exception>
        public static void Checkout(string branch) {
            Debug.Log("git fetch");
            CreateRequestGit("fetch").Execute();
            Debug.Log("Checkout branch " + branch);
            CreateRequestGit("checkout " + branch).Execute();
            Debug.Log("Checkout success");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Find commits, produced by <see cref="Revision"/>. 
        /// </summary>
        /// <param name="rev">Revision, produced by <see cref="Revision"/></param>
        /// <param name="len">Count of commits to write to console</param>
        /// <param name="branchForByNumber">If <i>rev</i> is number, <i>branchForByNumber</i> must be valid branch.
        /// If <i>rev</i> is short revision id, <i>branchForByNumber</i> must be <i>null</i>.</param>
        /// <returns>Information by found commits</returns>
        /// <exception cref="FormatException">Throws if <i>rev</i> is not valid</exception>
        /// <exception cref="ExternalException">Throws if git request failed</exception>
        /// <example>
        /// <code> FindRevision("234", 1, "master");</code>
        /// <code> FindRevision("1685c5a", 1, null);</code>
        /// </example>
        /// <seealso cref="Revision"/>
        public static string FindRevision(string rev, int len, string branchForByNumber) {
            string findStr;
            if (branchForByNumber != null) {
                var count = CreateRequestGit("rev-list --count " + branchForByNumber).Execute();
                try {
                    var number = int.Parse(count) - int.Parse(rev);
                    if (number < 0) throw new FormatException();
                    findStr = string.Format("{0} --skip={1}", branchForByNumber, number);
                } catch (FormatException) {
                    throw new FormatException("Not valid number");
                }
            } else {
                findStr = rev;
            }
            return CreateRequestGit("--no-pager log -" + len + " " + findStr).Execute();
        }

        private static ProgramRequest CreateRequestGit(string args = "") {
            return new ProgramRequest(BuildHelperStrings.GIT_EXEC_PATH, args);
        }
    }
}