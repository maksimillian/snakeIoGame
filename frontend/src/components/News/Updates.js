import React from 'react';
import styled from 'styled-components';

const UpdatesContainer = styled.div`
  max-width: 1200px;
  margin: 0 auto;
  padding: ${props => props.theme.spacing.large};
`;

const UpdatesGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: ${props => props.theme.spacing.large};
  margin-top: ${props => props.theme.spacing.large};
`;

const UpdateCard = styled.div`
  background-color: rgba(255,255,255,0.04); /* lighter than surface, works on dark themes */
  border-radius: ${props => props.theme.spacing.small};
  padding: ${props => props.theme.spacing.large};
  box-shadow: ${props => props.theme.shadows.small};
  transition: transform 0.3s ease, box-shadow 0.3s ease;

  &:hover {
    transform: translateY(-4px);
    box-shadow: ${props => props.theme.shadows.medium};
  }
`;

const UpdateDate = styled.div`
  color: ${props => props.theme.colors.textSecondary};
  font-size: ${props => props.theme.fontSizes.small};
  margin-bottom: ${props => props.theme.spacing.small};
`;

const UpdateTitle = styled.h3`
  color: ${props => props.theme.colors.text};
  margin-bottom: ${props => props.theme.spacing.medium};
  font-size: ${props => props.theme.fontSizes.large};
`;

const UpdateContent = styled.p`
  color: ${props => props.theme.colors.textSecondary};
  line-height: 1.6;
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const UpdateTag = styled.span`
  background-color: ${props => props.type === 'new' ? props.theme.colors.success : 
    props.type === 'update' ? props.theme.colors.info : 
    props.type === 'maintenance' ? props.theme.colors.warning : 
    props.theme.colors.primary};
  color: white;
  padding: ${props => props.theme.spacing.xsmall} ${props => props.theme.spacing.small};
  border-radius: ${props => props.theme.spacing.xsmall};
  font-size: ${props => props.theme.fontSizes.small};
  font-weight: 500;
`;

const updates = [
  {
    id: 1,
    date: 'March 16, 2024',
    title: 'New Neon Skins Collection',
    content: 'Introducing our latest collection of neon-themed snake skins! Glow in the dark and stand out in the arena with these vibrant new designs.',
    type: 'new'
  },
  {
    id: 2,
    date: 'March 15, 2024',
    title: 'Performance Improvements',
    content: 'We\'ve optimized the game engine for smoother gameplay and reduced latency. Experience better performance across all servers.',
    type: 'update'
  },
  {
    id: 3,
    date: 'March 14, 2024',
    title: 'Upcoming Tournament',
    content: 'Get ready for the Spring Championship! Starting next week, compete for exclusive rewards and the title of Snake.io Champion.',
    type: 'new'
  },
  {
    id: 4,
    date: 'March 13, 2024',
    title: 'Server Maintenance',
    content: 'Scheduled maintenance for Asia servers on March 20th. We\'ll be implementing new security features and server optimizations.',
    type: 'maintenance'
  },
  {
    id: 5,
    date: 'March 12, 2024',
    title: 'New Power-ups',
    content: 'Three new power-ups coming soon: Speed Boost, Shield, and Ghost Mode. Stay tuned for more details!',
    type: 'update'
  },
  {
    id: 6,
    date: 'March 11, 2024',
    title: 'Community Event',
    content: 'Join our first community event this weekend! Special rewards and exclusive skins for all participants.',
    type: 'new'
  }
];

function Updates() {
  return (
    <UpdatesContainer>
      <UpdatesGrid>
        {updates.map(update => (
          <UpdateCard key={update.id}>
            <UpdateDate>{update.date}</UpdateDate>
            <UpdateTitle>{update.title}</UpdateTitle>
            <UpdateContent>{update.content}</UpdateContent>
            <UpdateTag type={update.type}>
              {update.type === 'new' ? 'New' : 
               update.type === 'update' ? 'Update' : 
               update.type === 'maintenance' ? 'Maintenance' : 'News'}
            </UpdateTag>
          </UpdateCard>
        ))}
      </UpdatesGrid>
    </UpdatesContainer>
  );
}

export default Updates; 