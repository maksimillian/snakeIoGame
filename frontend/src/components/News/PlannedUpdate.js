import React from 'react';
import styled from 'styled-components';

const PlannedContainer = styled.div`
  max-width: 1200px;
  margin: 0 auto;
  padding: ${props => props.theme.spacing.large};
`;

const PlannedGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: ${props => props.theme.spacing.large};
  margin-top: ${props => props.theme.spacing.large};
`;

const PlannedCard = styled.div`
  background-color: rgba(255,255,255,0.04);
  border-radius: ${props => props.theme.spacing.small};
  padding: ${props => props.theme.spacing.large};
  box-shadow: ${props => props.theme.shadows.small};
  transition: transform 0.3s ease, box-shadow 0.3s ease;

  &:hover {
    transform: translateY(-4px);
    box-shadow: ${props => props.theme.shadows.medium};
  }
`;

const PlannedTitle = styled.h3`
  color: ${props => props.theme.colors.text};
  margin-bottom: ${props => props.theme.spacing.medium};
  font-size: ${props => props.theme.fontSizes.large};
`;

const PlannedContent = styled.p`
  color: ${props => props.theme.colors.textSecondary};
  line-height: 1.6;
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const PlannedTag = styled.span`
  background-color: ${props => props.theme.colors.warning};
  color: #222;
  padding: ${props => props.theme.spacing.xsmall} ${props => props.theme.spacing.small};
  border-radius: ${props => props.theme.spacing.xsmall};
  font-size: ${props => props.theme.fontSizes.small};
  font-weight: 500;
`;

const plannedUpdates = [
  {
    id: 1,
    title: 'Classic Mode',
    content: 'Traditional snake gameplay.',
    date: 'Planned Q2 2024'
  },
  {
    id: 2,
    title: 'Battle Royale',
    content: 'Last snake standing wins.',
    date: 'Planned Q2 2024'
  },
  {
    id: 3,
    title: 'Team Deathmatch',
    content: 'Compete in teams.',
    date: 'Planned Q2 2024'
  },
  {
    id: 4,
    title: 'Custom Rooms',
    content: 'Create private games with friends.',
    date: 'Planned Q2 2024'
  },
];

function PlannedUpdate() {
  return (
    <PlannedContainer>
      <PlannedGrid>
        {plannedUpdates.map(update => (
          <PlannedCard key={update.id}>
            <PlannedTitle>{update.title}</PlannedTitle>
            <PlannedContent>{update.content}</PlannedContent>
            <PlannedTag>Planned</PlannedTag>
            <div style={{ color: '#b0b0b0', fontSize: '14px', marginTop: '8px' }}>{update.date}</div>
          </PlannedCard>
        ))}
      </PlannedGrid>
    </PlannedContainer>
  );
}

export default PlannedUpdate; 