mergeInto(LibraryManager.library, {

  InitializeSDK: function(authClientIdPtr, envPtr) {
    const authClientId = UTF8ToString(authClientIdPtr);
    const env = UTF8ToString(envPtr);

    if (typeof FunticoSDK === 'undefined') {
      console.error("FunticoSDK is not loaded. Add the SDK <script> to your index.html.");
      return;
    }
    funticoSDKInstance = new FunticoSDK({
      authClientId: authClientId,
      env: env
    });
    console.log("Funtico SDK Initialized.");
  },

  SignIn: function(gameObjectNamePtr, promiseId) {
    // Safety check for both the SDK and the Unity instance
    if (!funticoSDKInstance || !myGameInstance) return;
    const gameObjectName = UTF8ToString(gameObjectNamePtr);
    
    funticoSDKInstance.signInWithFuntico(window.location.href)
      .then(() => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'ResolvePromise', `${promiseId}:true`);
      })
      .catch(error => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'RejectPromise', `${promiseId}:${JSON.stringify(error)}`);
      });
  },

  GetUserInfo: function(gameObjectNamePtr, promiseId) {
    // Safety check for both the SDK and the Unity instance
    if (!funticoSDKInstance || !myGameInstance) return;
    const gameObjectName = UTF8ToString(gameObjectNamePtr);

    funticoSDKInstance.getUserInfo()
      .then(userInfo => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'ResolvePromise', `${promiseId}:${JSON.stringify(userInfo)}`);
      })
      .catch(error => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'RejectPromise', `${promiseId}:${JSON.stringify(error)}`);
      });
  },

  SaveScore: function(score, gameObjectNamePtr, promiseId) {
    // Safety check for both the SDK and the Unity instance
    if (!funticoSDKInstance || !myGameInstance) return;
    const gameObjectName = UTF8ToString(gameObjectNamePtr);
    
    funticoSDKInstance.saveScore(score)
      .then(response => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'ResolvePromise', `${promiseId}:${JSON.stringify(response)}`);
      })
      .catch(error => {
        // UPDATED to use myGameInstance
        myGameInstance.SendMessage(gameObjectName, 'RejectPromise', `${promiseId}:${JSON.stringify(error)}`);
      });
  },

  SignOut: function() {
    if (!funticoSDKInstance) return;
    funticoSDKInstance.signOut(window.location.href);
  },

  ShowAlert: function(messagePtr) {
    const message = UTF8ToString(messagePtr);
    alert(message);
  },
});