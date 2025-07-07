import React from 'react';
import WebGLContainer from '../components/GameFrame/WebGLContainer';
import './GamePage.css';

const GamePage = () => {
  return (
    <div className="game-page">
      <div className="game-header">
        <div className="game-title">
          <h1>Snake.io</h1>
          <p>Multiplayer Snake Game</p>
        </div>
        <div className="game-controls">
          <button className="back-button" onClick={() => window.history.back()}>
            ← Back to Menu
          </button>
        </div>
      </div>
      
      <div className="game-content">
        <WebGLContainer />
      </div>
      
      <div className="game-footer">
        <div className="game-info">
          <span>Use mouse to control your snake</span>
          <span>•</span>
          <span>Left click to boost</span>
          <span>•</span>
          <span>Eat food to grow</span>
        </div>
      </div>
    </div>
  );
};

export default GamePage; 