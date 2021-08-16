using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Unity.Netcode.Transports.UNET;

namespace Unity.Netcode.MultiprocessRuntimeTests
{
    public class MultiprocessTestsAttribute : CategoryAttribute
    {
        
        public const string MultiprocessCategoryName = "Multiprocess";
        public MultiprocessTestsAttribute() : base(MultiprocessCategoryName) { }
    }

    [MultiprocessTests]
    public abstract class BaseMultiprocessTests
    {
        protected virtual bool IsPerformanceTest => true;
        private string m_Port = "3076"; // TODO This port will need to be reconfigurable
        private const string k_GlobalEmptySceneName = "EmptyScene";

        private bool m_SceneHasLoaded;

        protected bool ShouldIgnoreTests => IsPerformanceTest && Application.isEditor;

        /// <summary>
        /// Implement this to specify the amount of workers to spawn from your main test runner
        /// TODO there's a good chance this will be refactored with something fancier once we start integrating with bokken
        /// </summary>
        protected abstract int WorkerCount { get; }

        [OneTimeSetUp]
        public virtual void SetupTestSuite()
        {
            Debug.Log($"Setting port in OneTimeSetUp to {m_Port}");
            // SetPort(ushort.Parse(m_Port));
            Debug.Log($"Port set to {m_Port}");
            if (ShouldIgnoreTests)
            {
                Assert.Ignore("Ignoring tests that shouldn't run from unity editor. Performance tests should be run from remote test execution on device (this can be ran using the \"run selected tests (your platform)\" button");
            }
            Debug.Log("Loading Scene");
            SceneManager.LoadScene(BuildMultiprocessTestPlayer.MainSceneName, LoadSceneMode.Single);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            NetworkManager.Singleton.StartHost();
            for (int i = 0; i < WorkerCount; i++)
            {
                MultiprocessOrchestration.StartWorkersOnRemoteNodes(); // will automatically start built player as clients
            }

            m_SceneHasLoaded = true;
        }

        [UnitySetUp]
        public virtual IEnumerator Setup()
        {
            
            SetPort(ushort.Parse(m_Port));
            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && m_SceneHasLoaded);

            var startTime = Time.time;
            while (NetworkManager.Singleton.ConnectedClients.Count <= WorkerCount)
            {
                yield return new WaitForSeconds(10.0f);

                if (Time.time - startTime > TestCoordinator.MaxWaitTimeoutSec)
                {
                    throw new Exception($"waiting too long to see clients to connect, got {NetworkManager.Singleton.ConnectedClients.Count - 1} clients, but was expecting {WorkerCount}, failing");
                }
                Debug.LogWarning($"Connected client count < WorkerCount, {NetworkManager.Singleton.ConnectedClients.Count } < {WorkerCount} ");
            }

            TestCoordinator.Instance.KeepAliveClientRpc();
        }

        [TearDown]
        public virtual void Teardown()
        {
            if (!ShouldIgnoreTests)
            {
                TestCoordinator.Instance.TestRunTeardown();
            }
        }

        [OneTimeTearDown]
        public virtual void TeardownSuite()
        {
            if (!ShouldIgnoreTests)
            {
                TestCoordinator.Instance.CloseRemoteClientRpc();
                NetworkManager.Singleton.StopHost();
                Object.Destroy(NetworkManager.Singleton.gameObject); // making sure we clear everything before reloading our scene
                SceneManager.LoadScene(k_GlobalEmptySceneName); // using empty scene to clear our state
            }
        }

        private void SetPort(ushort port)
        {
            
            Debug.Log($"Getting network manager {NetworkManager.Singleton}");
            Debug.Log($"Getting network manager {NetworkManager.Singleton.NetworkConfig}");
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            Debug.Log($"Got network manager network transport as {transport}");

            switch (transport)
            {
                case UNetTransport unetTransport:
                    unetTransport.ConnectPort = port;
                    unetTransport.ServerListenPort = port;
                    break;
            }
        }
    }
}

