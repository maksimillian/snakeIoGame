# Unity WebGL Setup Guide

## Step 1: Build Unity to WebGL

1. **Open Unity Hub** and open your `SnakeioClient` project
2. **Go to File → Build Settings**
3. **Select "WebGL"** as the platform (click "Switch Platform" if needed)
4. **Set build location** to: `frontend/public/game/`
5. **Click "Build"** and wait for the build to complete

## ⚠️ IMPORTANT: Compression Settings

**To avoid Brotli compression issues, use these Unity build settings:**

- **Compression Format**: **Disabled** (this is crucial!)
- **Development Build**: Checked (for debugging)
- **WebGL Memory**: 512MB (adjust as needed)
- **WebGL Template**: Default

**Why?** Unity WebGL uses Brotli compression (`.br` files) which requires HTTPS or proper server configuration. Disabling compression avoids this issue entirely.

## Step 2: Create Directory Structure

```bash
mkdir -p frontend/public/game
```

## Step 3: Copy WebGL Build Files

After Unity builds, copy all files from the build folder to `frontend/public/game/`. The structure should look like:

```
frontend/public/game/
├── Build/
│   ├── UnityLoader.js
│   ├── SnakeioClient.json (or your build name)
│   └── SnakeioClient.data
├── StreamingAssets/
└── TemplateData/
```

## Step 4: Update Build Path

In `frontend/src/components/GameFrame/WebGLContainer.js`, update line 67:

```javascript
// Change this line to match your actual build file name
'/game/Build/SnakeioClient.json'  // Replace with your actual build file name
```

## Step 5: Test the Setup

1. **Start the React development server:**
   ```bash
   cd frontend
   npm start
   ```

2. **Navigate to:** `http://localhost:3000/play`

3. **The game should load** with the Unity WebGL build

## Troubleshooting

### Common Issues:

1. **"Unable to parse Build/game.framework.js.br!"**
   - **Solution**: Rebuild Unity with **Compression Format: Disabled**
   - This is the most common issue with Unity WebGL builds

2. **"Unity WebGL loader not found"**
   - Check that `UnityLoader.js` is in `frontend/public/game/Build/`
   - Verify the path in `WebGLContainer.js`

3. **"Failed to load Unity WebGL loader script"**
   - Make sure all build files are copied to the correct location
   - Check browser console for 404 errors

4. **Build file not found**
   - Update the build path in `WebGLContainer.js` to match your actual build file name
   - Unity build files usually have names like `SnakeioClient.json` or similar

5. **CORS issues**
   - Make sure you're running the React dev server (`npm start`)
   - Don't open the HTML file directly in the browser

### Unity Build Settings (Recommended):

- **Compression Format**: **Disabled** ⚠️ (Most important!)
- **Development Build**: Checked
- **WebGL Memory**: 512MB
- **WebGL Template**: Default
- **Run In Background**: Checked
- **WebGL Exception Support**: Full (for debugging)

### Alternative Solutions for Compression Issues:

If you want to keep compression enabled:

1. **Use HTTPS in development:**
   ```bash
   # Install mkcert for local HTTPS
   npm install -g mkcert
   mkcert -install
   mkcert localhost
   
   # Start React with HTTPS
   HTTPS=true SSL_CRT_FILE=localhost.pem SSL_KEY_FILE=localhost-key.pem npm start
   ```

2. **Use a local HTTPS server:**
   ```bash
   npx serve -s build --ssl-cert localhost.pem --ssl-key localhost-key.pem
   ```

## File Structure After Setup:

```
frontend/
├── public/
│   └── game/
│       ├── Build/
│       │   ├── UnityLoader.js
│       │   ├── SnakeioClient.json
│       │   └── SnakeioClient.data
│       ├── StreamingAssets/
│       └── TemplateData/
└── src/
    ├── components/
    │   └── GameFrame/
    │       ├── WebGLContainer.js
    │       └── WebGLContainer.css
    └── pages/
        ├── GamePage.js
        └── GamePage.css
```

## Testing Checklist:

- [ ] Unity WebGL build completed successfully with **compression disabled**
- [ ] Build files copied to `frontend/public/game/`
- [ ] Build path updated in `WebGLContainer.js`
- [ ] React dev server running (`npm start`)
- [ ] Game loads at `http://localhost:3000/play`
- [ ] No compression errors in browser console
- [ ] Unity game starts and connects to server
- [ ] Test room join button works
- [ ] Snake movement and controls work
- [ ] Fullscreen button works 