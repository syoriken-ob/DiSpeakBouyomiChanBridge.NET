using net.boilingwater.Framework.Common.Initialize;
using net.boilingwater.Framework.Common.Logging;

namespace net.boilingwater.VoiceVoxProxy
{
    internal class ApplicationInitializer
    {
        /// <summary>
        /// システムの初期化を行います
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        internal static void Initialize()
        {
            CommonInitializer.Initialize();
            Log.Logger.Info("アプリケーションの初期化処理を開始します。");

            InitializeHttpServer();
            InitializeHttpClinet();

            Log.Logger.Info("アプリケーションの初期化処理が完了しました。");
        }

        /// <summary>
        /// HttpServerの初期化します
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        private static void InitializeHttpServer()
        {
            //HttpServerForCommon.Instance.Initialize();
        }

        /// <summary>
        ///
        /// </summary>
        private static void InitializeHttpClinet()
        {
        }

        /// <summary>
        /// 処理を開始します
        /// </summary>
        internal static void Start()
        {
            //HttpServerForCommon.Instance.Start();
        }
    }
}
