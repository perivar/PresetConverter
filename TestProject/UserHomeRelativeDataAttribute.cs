using System.Reflection;
using Xunit.Sdk;

namespace TestProject
{
    /// <summary>
    /// Provides test data paths relative to the user's home directory.
    /// Expects parameters as full relative paths from the user's home directory, using '/' as a directory separator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class UserHomeRelativeDataAttribute : DataAttribute
    {
        private readonly string[] _relativePaths; // Store full relative paths from user's home directory

        /// <summary>
        /// Initializes a new instance of the <see cref="UserHomeRelativeDataAttribute"/> class.
        /// </summary>
        /// <param name="relativePaths">Full relative paths from the user's home directory,
        /// using '/' as a directory separator, e.g., "/OneDrive/Folder/SubFolder/file.ext".</param>
        public UserHomeRelativeDataAttribute(params string[] relativePaths)
        {
            if (relativePaths == null || relativePaths.Length == 0)
            {
                throw new ArgumentException("At least one relative path must be provided.", nameof(relativePaths));
            }
            _relativePaths = relativePaths;
        }

        /// <inheritdoc />
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var testCaseArguments = new object[_relativePaths.Length];
            for (int i = 0; i < _relativePaths.Length; i++)
            {
                string fullRelativePath = _relativePaths[i];

                // Normalize the relative path
                string normalizedRelativePath = fullRelativePath.TrimStart('/');
                normalizedRelativePath = normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar);

                string finalPath = Path.Combine(homeDirectory, normalizedRelativePath);
                testCaseArguments[i] = finalPath;
            }

            yield return testCaseArguments; // Return a single object[] for one test case
        }
    }
}