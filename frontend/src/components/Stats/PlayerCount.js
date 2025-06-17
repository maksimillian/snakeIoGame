import React, { useEffect, useState } from 'react';
import styled, { keyframes } from 'styled-components';
import { FaUserFriends } from 'react-icons/fa';

const CountUp = keyframes`
  from { opacity: 0; transform: scale(0.8); }
  to { opacity: 1; transform: scale(1); }
`;

const PlayerCountContainer = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: rgba(255,255,255,0.03);
  border-radius: 40px;
  padding: 80px 48px 64px 48px;
  margin: 0 auto 56px auto;
  max-width: 700px;
  box-shadow: 0 6px 32px rgba(0,0,0,0.15);
`;

const Caption = styled.div`
  color: ${props => props.theme.colors.text};
  font-size: 2.1rem;
  font-weight: 700;
  margin-bottom: 32px;
  text-align: center;
`;

const IconCircle = styled.div`
  background: linear-gradient(135deg, #4CAF50 0%, #2196F3 100%);
  border-radius: 50%;
  width: 128px;
  height: 128px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 32px;
`;

const Count = styled.div`
  font-size: 6rem;
  font-weight: 900;
  color: ${props => props.theme.colors.primary};
  letter-spacing: 3px;
  margin-bottom: 16px;
  animation: ${CountUp} 0.7s cubic-bezier(0.4,0,0.2,1);
`;

const Label = styled.div`
  color: ${props => props.theme.colors.textSecondary};
  font-size: 1.4rem;
  margin-bottom: 24px;
`;

const MilestoneBar = styled.div`
  width: 100%;
  height: 14px;
  background: ${props => props.theme.colors.surface};
  border-radius: 7px;
  overflow: hidden;
  margin-top: 18px;
`;

const MilestoneFill = styled.div`
  height: 100%;
  background: linear-gradient(90deg, #4CAF50 0%, #2196F3 100%);
  width: ${props => props.percent}%;
  transition: width 1s cubic-bezier(0.4,0,0.2,1);
`;

const MilestoneText = styled.div`
  color: ${props => props.theme.colors.textSecondary};
  font-size: 1.1rem;
  margin-top: 12px;
  text-align: center;
`;

const START_COUNT = 8542;
const START_TIMESTAMP = Date.UTC(2025, 5, 17, 0, 0, 0); // June 17, 2025 00:00 UTC (month is 0-based)
const USERS_PER_HOUR = 7;
const MILESTONE = 10000;

function getCurrentPlayerCount() {
  const now = Date.now();
  const hoursPassed = Math.floor((now - START_TIMESTAMP) / (1000 * 60 * 60));
  return START_COUNT + Math.max(0, hoursPassed * USERS_PER_HOUR);
}

function PlayerCount() {
  const [displayCount, setDisplayCount] = useState(0);
  const [targetCount, setTargetCount] = useState(getCurrentPlayerCount());

  useEffect(() => {
    setTargetCount(getCurrentPlayerCount());
    let start = 0;
    const end = getCurrentPlayerCount();
    if (start === end) return;
    let increment = Math.ceil(end / 60);
    let current = start;
    const timer = setInterval(() => {
      current += increment;
      if (current >= end) {
        current = end;
        clearInterval(timer);
      }
      setDisplayCount(current);
    }, 10);
    return () => clearInterval(timer);
  }, []);

  const percent = Math.min(100, Math.round((targetCount / MILESTONE) * 100));

  return (
    <PlayerCountContainer>
      <Caption>There are already <span style={{color: '#4CAF50'}}>{displayCount.toLocaleString()}</span> of us</Caption>
      <IconCircle>
        <FaUserFriends size={64} color="#fff" />
      </IconCircle>
      <Count>{displayCount.toLocaleString()}</Count>
      <Label>Registered Players</Label>
      <MilestoneBar>
        <MilestoneFill percent={percent} />
      </MilestoneBar>
      <MilestoneText>
        Next milestone: <b>{MILESTONE.toLocaleString()}</b> players
      </MilestoneText>
    </PlayerCountContainer>
  );
}

export default PlayerCount; 