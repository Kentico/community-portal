using CMS.DataEngine;

namespace Kentico.Community.Portal.Core.Modules
{
    /// <summary>
    /// Class providing <see cref="QAndAAnswerDataInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IQAndAAnswerDataInfoProvider))]
    public partial class QAndAAnswerDataInfoProvider : AbstractInfoProvider<QAndAAnswerDataInfo, QAndAAnswerDataInfoProvider>, IQAndAAnswerDataInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QAndAAnswerDataInfoProvider"/> class.
        /// </summary>
        public QAndAAnswerDataInfoProvider()
            : base(QAndAAnswerDataInfo.TYPEINFO)
        {
        }
    }
}