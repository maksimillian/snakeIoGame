import React from 'react';
import styled from 'styled-components';

const PolicyContainer = styled.div`
  max-width: 800px;
  margin: 0 auto;
  padding: ${props => props.theme.spacing.large};
`;

const Section = styled.section`
  margin-bottom: ${props => props.theme.spacing.xlarge};
`;

const SectionTitle = styled.h2`
  color: ${props => props.theme.colors.primary};
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const Paragraph = styled.p`
  margin-bottom: ${props => props.theme.spacing.medium};
  line-height: 1.8;
  color: ${props => props.theme.colors.textSecondary};
`;

const List = styled.ul`
  list-style-type: none;
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const ListItem = styled.li`
  margin-bottom: ${props => props.theme.spacing.small};
  padding-left: ${props => props.theme.spacing.medium};
  position: relative;
  color: ${props => props.theme.colors.textSecondary};

  &:before {
    content: "•";
    color: ${props => props.theme.colors.primary};
    position: absolute;
    left: 0;
  }
`;

function PrivacyPolicy() {
  return (
    <PolicyContainer>
      <Section>
        <SectionTitle>Privacy Policy</SectionTitle>
        <Paragraph>
          Last updated: March 16, 2024
        </Paragraph>
        <Paragraph>
          Welcome to Snake.io. We respect your privacy and are committed to protecting your personal data. This privacy policy will inform you about how we look after your personal data when you visit our website and tell you about your privacy rights.
        </Paragraph>
      </Section>

      <Section>
        <SectionTitle>Information We Collect</SectionTitle>
        <Paragraph>We collect several types of information for various purposes:</Paragraph>
        <List>
          <ListItem>Account information (username, email, password)</ListItem>
          <ListItem>Game statistics and achievements</ListItem>
          <ListItem>Device information and IP address</ListItem>
          <ListItem>Usage data and preferences</ListItem>
        </List>
      </Section>

      <Section>
        <SectionTitle>How We Use Your Information</SectionTitle>
        <Paragraph>We use the collected information for various purposes:</Paragraph>
        <List>
          <ListItem>To provide and maintain our Service</ListItem>
          <ListItem>To notify you about changes to our Service</ListItem>
          <ListItem>To provide customer support</ListItem>
          <ListItem>To gather analysis or valuable information</ListItem>
          <ListItem>To monitor the usage of our Service</ListItem>
          <ListItem>To detect, prevent and address technical issues</ListItem>
        </List>
      </Section>

      <Section>
        <SectionTitle>Data Security</SectionTitle>
        <Paragraph>
          The security of your data is important to us. We implement appropriate technical and organizational measures to protect your personal data against unauthorized or unlawful processing, accidental loss, destruction, or damage.
        </Paragraph>
      </Section>

      <Section>
        <SectionTitle>Your Rights</SectionTitle>
        <Paragraph>You have the following rights regarding your personal data:</Paragraph>
        <List>
          <ListItem>Right to access your personal data</ListItem>
          <ListItem>Right to rectification of inaccurate data</ListItem>
          <ListItem>Right to erasure of your data</ListItem>
          <ListItem>Right to restrict processing</ListItem>
          <ListItem>Right to data portability</ListItem>
          <ListItem>Right to object to processing</ListItem>
        </List>
      </Section>

      <Section>
        <SectionTitle>Contact Us</SectionTitle>
        <Paragraph>
          If you have any questions about this Privacy Policy, please contact us at:
          <br />
          Email: privacy@snakeio.com
          <br />
          Discord: discord.gg/snakeio
        </Paragraph>
      </Section>
    </PolicyContainer>
  );
}

export default PrivacyPolicy; 