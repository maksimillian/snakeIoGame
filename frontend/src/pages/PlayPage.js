import React, { useState } from 'react';
import styled from 'styled-components';
import Updates from '../components/News/Updates';

const PlayContainer = styled.div`
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: ${props => props.theme.colors.background};
  align-items: center;
  justify-content: center;
`;

const HeroSection = styled.section`
  position: relative;
  height: 100vh;
  width: 100%;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  background: ${props => props.theme.colors.background};
  overflow: hidden;
  padding: 0;
  margin: 0;
`;

const GameContainer = styled.div`
  width: 100%;
  height: 100%;
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 0;
  margin: 0;
`;

const GameFrame = styled.iframe`
  width: 100%;
  height: 100%;
  border: none;
  background: ${props => props.theme.colors.surface};
  display: block;
`;

const LoadingOverlay = styled.div`
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: ${props => props.theme.colors.background};
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  z-index: 10;
  transition: opacity 0.3s ease;
  opacity: ${props => props.isLoading ? 1 : 0};
  pointer-events: ${props => props.isLoading ? 'auto' : 'none'};
`;

const LoadingText = styled.h2`
  color: ${props => props.theme.colors.text};
  margin-bottom: ${props => props.theme.spacing.large};
  font-size: ${props => props.theme.fontSizes.xlarge};
  text-align: center;
`;

const ProgressBar = styled.div`
  width: 200px;
  height: 4px;
  background: ${props => props.theme.colors.surface};
  border-radius: 2px;
  overflow: hidden;
`;

const Progress = styled.div`
  width: ${props => props.progress}%;
  height: 100%;
  background: ${props => props.theme.colors.gradient};
  transition: width 0.3s ease;
`;

const PlayButton = styled.button`
  background: ${props => props.theme.colors.gradient};
  color: white;
  border: none;
  padding: ${props => props.theme.spacing.medium} ${props => props.theme.spacing.xlarge};
  border-radius: ${props => props.theme.spacing.small};
  font-size: ${props => props.theme.fontSizes.large};
  font-weight: 600;
  cursor: pointer;
  margin-top: ${props => props.theme.spacing.large};
  transition: transform 0.3s ease, box-shadow 0.3s ease;
  box-shadow: ${props => props.theme.shadows.medium};

  &:hover {
    transform: translateY(-2px);
    box-shadow: ${props => props.theme.shadows.large};
  }
`;

const UpdatesSection = styled.section`
  padding: ${props => props.theme.spacing.xxlarge} 0;
  background: ${props => props.theme.colors.surface};
`;

const SectionTitle = styled.h2`
  text-align: center;
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.xxlarge};
  margin-bottom: ${props => props.theme.spacing.xlarge};
`;

const Warning = styled.div`
  color: ${props => props.theme.colors.error};
  background: #1a1a1a;
  border: 1px solid ${props => props.theme.colors.error};
  padding: ${props => props.theme.spacing.large};
  border-radius: ${props => props.theme.spacing.small};
  margin: ${props => props.theme.spacing.large} 0;
  text-align: center;
  max-width: 500px;
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
`;

function PlayPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [progress, setProgress] = useState(0);
  const [gameStatus, setGameStatus] = useState('checking'); // 'checking', 'unity', 'react', 'missing'

  React.useEffect(() => {
    // Check /game/index.html before rendering iframe
    fetch('/game/index.html', { method: 'GET' })
      .then(res => {
        if (!res.ok) throw new Error('Not found');
        return res.text();
      })
      .then(text => {
        if (text.includes('id="root"') || text.includes('Snake.io')) {
          setGameStatus('react');
        } else if (text.includes('UnityLoader') || text.includes('UnityProgress') || text.includes('unityInstance')) {
          setGameStatus('unity');
        } else {
          setGameStatus('unknown');
        }
      })
      .catch(() => setGameStatus('missing'));
  }, []);

  React.useEffect(() => {
    if (gameStatus !== 'unity') return;
    // Simulate loading progress
    const interval = setInterval(() => {
      setProgress(prev => {
        if (prev >= 100) {
          clearInterval(interval);
          setIsLoading(false);
          return 100;
        }
        return prev + 10;
      });
    }, 500);
    return () => clearInterval(interval);
  }, [gameStatus]);

  if (gameStatus === 'checking') {
    return (
      <PlayContainer>
        <LoadingText>Checking for game...</LoadingText>
      </PlayContainer>
    );
  }

  if (gameStatus === 'react') {
    return (
      <PlayContainer>
        <Warning>
          <b>Warning:</b> The Unity WebGL build should be placed in <code>public/game/index.html</code>.<br />
          You are currently viewing the React app inside the game frame, which causes recursion.<br />
          Please replace <code>public/game/index.html</code> with your Unity WebGL build.
        </Warning>
      </PlayContainer>
    );
  }

  if (gameStatus === 'missing') {
    return (
      <PlayContainer>
        <Warning>
          <b>Game not found:</b> Please place your Unity WebGL build in <code>public/game/index.html</code>.<br />
          The game could not be loaded.
        </Warning>
      </PlayContainer>
    );
  }

  return (
    <PlayContainer>
      <HeroSection>
        <GameContainer>
          <GameFrame
            src="/game/index.html"
            title="Snake.io Game"
            onLoad={() => setIsLoading(false)}
            sandbox="allow-same-origin allow-scripts allow-popups allow-forms"
          />
          <LoadingOverlay isLoading={isLoading}>
            <LoadingText>Loading Snake.io</LoadingText>
            <ProgressBar>
              <Progress progress={progress} />
            </ProgressBar>
            {progress === 100 && (
              <PlayButton onClick={() => setIsLoading(false)}>
                Play Now
              </PlayButton>
            )}
          </LoadingOverlay>
        </GameContainer>
      </HeroSection>
      <UpdatesSection>
        <SectionTitle>Latest Updates</SectionTitle>
        <Updates />
      </UpdatesSection>
    </PlayContainer>
  );
}

export default PlayPage; 