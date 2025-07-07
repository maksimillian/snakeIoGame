import React, { useEffect, useRef, useState, useCallback } from 'react';
import './WebGLContainer.css';

const WebGLContainer = () => {
  const containerRef = useRef(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [compressionError, setCompressionError] = useState(false);

  const loadUnityWebGL = useCallback(() => {
    try {
      setIsLoading(true);
      setError(null);
      setCompressionError(false);

      // Create Unity WebGL container
      const unityContainer = document.createElement('div');
      unityContainer.id = 'unity-container';
      unityContainer.className = 'unity-container';

      // Create Unity canvas
      const unityCanvas = document.createElement('canvas');
      unityCanvas.id = 'unity-canvas';
      unityCanvas.className = 'unity-canvas';
      unityContainer.appendChild(unityCanvas);

      // Create Unity loading bar
      const loadingBar = document.createElement('div');
      loadingBar.id = 'unity-loading-bar';
      loadingBar.className = 'unity-loading-bar';
      
      const loadingBarFill = document.createElement('div');
      loadingBarFill.id = 'unity-loading-bar-fill';
      loadingBarFill.className = 'unity-loading-bar-fill';
      loadingBar.appendChild(loadingBarFill);
      
      unityContainer.appendChild(loadingBar);

      // Add to container
      if (containerRef.current) {
        containerRef.current.appendChild(unityContainer);
      }

      // Load Unity WebGL
      loadUnityBuild();

    } catch (err) {
      setError('Failed to initialize Unity WebGL: ' + err.message);
      setIsLoading(false);
    }
  }, []);

  const loadUnityBuild = useCallback(() => {
    // Check if we're on HTTPS or localhost
    const isSecure = window.location.protocol === 'https:' || window.location.hostname === 'localhost';
    
    if (!isSecure) {
      setCompressionError(true);
      setError('HTTPS Required: Unity WebGL requires HTTPS for Brotli compression. Please use HTTPS or localhost.');
      setIsLoading(false);
      return;
    }

    // Add console logging for debugging
    console.log('Loading Unity WebGL build...');
    console.log('Current protocol:', window.location.protocol);
    console.log('Current hostname:', window.location.hostname);
    console.log('Is secure context:', isSecure);

    // Load Socket.IO client library first
    const socketIOScript = document.createElement('script');
    socketIOScript.src = 'https://cdn.socket.io/4.7.2/socket.io.min.js';
    socketIOScript.onload = () => {
      console.log('Socket.IO client library loaded successfully');
      
      // Unity WebGL loader script (newer format)
      const script = document.createElement('script');
      script.src = '/game/Build/game.loader.js';
      script.onload = () => {
        console.log('Unity game.loader.js loaded successfully');
        
        // Initialize Unity with the newer format
        if (window.createUnityInstance) {
          try {
            console.log('Initializing Unity with game configuration...');
            
            const config = {
              dataUrl: '/game/Build/game.data',
              frameworkUrl: '/game/Build/game.framework.js',
              codeUrl: '/game/Build/game.wasm',
              streamingAssetsUrl: '/game/StreamingAssets',
              companyName: 'DefaultCompany',
              productName: 'SnakeioClient',
              productVersion: '1.0',
              showBanner: (msg, type) => {
                console.log('Unity Banner:', msg, type);
                if (type === 'error') {
                  setError('Unity Error: ' + msg);
                  setIsLoading(false);
                }
              },
            };
            
            window.createUnityInstance(
              document.getElementById('unity-canvas'),
              config,
              (progress) => {
                console.log('Unity loading progress:', progress);
                const loadingBarFill = document.getElementById('unity-loading-bar-fill');
                if (loadingBarFill) {
                  loadingBarFill.style.width = (progress * 100) + '%';
                }
              }
            ).then((unityInstance) => {
              console.log('Unity WebGL loaded successfully!');
              setIsLoading(false);
              const loadingBar = document.getElementById('unity-loading-bar');
              if (loadingBar) {
                loadingBar.style.display = 'none';
              }
              
              // Store the unity instance for potential use
              window.unityInstance = unityInstance;
              
              // Add event listener for Unity console messages
              window.addEventListener('message', (event) => {
                if (event.data && event.data.type === 'unity-console') {
                  console.log('Unity Console:', event.data.message);
                }
              });
            }).catch((error) => {
              console.error('Unity WebGL Error:', error);
              
              // Check for compression-related errors
              if (error && (error.toString().includes('br') || error.toString().includes('Brotli'))) {
                setCompressionError(true);
                setError('Compression Error: Brotli compression not supported. Please rebuild Unity with compression disabled or use HTTPS.');
              } else {
                setError('Unity WebGL failed to load: ' + error);
              }
              setIsLoading(false);
            });
          } catch (err) {
            console.error('Unity instantiation error:', err);
            setError('Failed to instantiate Unity: ' + err.message);
            setIsLoading(false);
          }
        } else {
          setError('Unity WebGL loader not found');
          setIsLoading(false);
        }
      };
      script.onerror = () => {
        console.error('Failed to load game.loader.js');
        setError('Failed to load Unity WebGL loader script. Check if build files are in the correct location.');
        setIsLoading(false);
      };
      document.head.appendChild(script);
    };
    socketIOScript.onerror = () => {
      console.error('Failed to load Socket.IO client library');
      setError('Failed to load Socket.IO client library. This is required for game networking.');
      setIsLoading(false);
    };
    document.head.appendChild(socketIOScript);
  }, []);

  useEffect(() => {
    loadUnityWebGL();
  }, [loadUnityWebGL]);

  const toggleFullscreen = () => {
    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen();
      setIsFullscreen(true);
    } else {
      document.exitFullscreen();
      setIsFullscreen(false);
    }
  };

  const retryLoad = () => {
    if (containerRef.current) {
      containerRef.current.innerHTML = '';
    }
    loadUnityWebGL();
  };

  const showCompressionHelp = () => {
    const helpText = `
Unity WebGL Build Issues:

1. **HTTPS Required**: Unity WebGL uses Brotli compression which requires HTTPS
2. **Rebuild Options**:
   - Rebuild Unity with compression disabled
   - Use HTTPS in development
   - Use localhost (which allows compression)

3. **Quick Fix**: Rebuild Unity WebGL with these settings:
   - Compression Format: Disabled
   - Development Build: Checked
   - WebGL Memory: 512MB
   - WebGL Template: Default

4. **Alternative**: Use a local HTTPS server for development
    `;
    alert(helpText);
  };

  return (
    <div className="webgl-container" ref={containerRef}>
      {isLoading && !error && (
        <div className="loading-overlay">
          <div className="loading-spinner"></div>
          <p>Loading Snake.io Game...</p>
        </div>
      )}
      
      {error && (
        <div className="error-overlay">
          <div className="error-content">
            <h3>Game Loading Error</h3>
            <p>{error}</p>
            
            {compressionError && (
              <div className="compression-help">
                <p><strong>This is likely a compression issue.</strong></p>
                <button onClick={showCompressionHelp} className="help-button">
                  Show Help
                </button>
              </div>
            )}
            
            <div className="error-actions">
              <button onClick={retryLoad} className="retry-button">
                Retry
              </button>
              {compressionError && (
                <button onClick={() => window.location.href = '/game'} className="back-button">
                  Back to Menu
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {!isLoading && !error && (
        <button 
          onClick={toggleFullscreen} 
          className="fullscreen-button"
          title={isFullscreen ? "Exit Fullscreen" : "Enter Fullscreen"}
        >
          {isFullscreen ? "⤓" : "⤢"}
        </button>
      )}
    </div>
  );
};

export default WebGLContainer; 