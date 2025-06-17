import React, { useState, useRef, useEffect } from 'react';
import styled from 'styled-components';
import Updates from '../components/News/Updates';
import PlannedUpdate from '../components/News/PlannedUpdate';
import PlayerCount from '../components/Stats/PlayerCount';

const InfoContainer = styled.div`
  padding: ${props => props.theme.spacing.xxlarge} 0;
  background: ${props => props.theme.colors.background};
`;

const Section = styled.section`
  max-width: 1200px;
  margin: 0 auto;
  padding: ${props => props.theme.spacing.xlarge};
  background: ${props => props.theme.colors.surface};
  border-radius: ${props => props.theme.spacing.medium};
  box-shadow: ${props => props.theme.shadows.medium};
  margin-bottom: ${props => props.theme.spacing.xlarge};

  &:last-child {
    margin-bottom: 0;
  }
`;

const SectionTitle = styled.h2`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.xxlarge};
  margin-bottom: ${props => props.theme.spacing.large};
  text-align: center;
`;

const SubSection = styled.div`
  margin-bottom: ${props => props.theme.spacing.xlarge};

  &:last-child {
    margin-bottom: 0;
  }
`;

const SubTitle = styled.h3`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.xlarge};
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const Text = styled.p`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.medium};
  line-height: 1.6;
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const FeatureList = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
`;

const FeatureItem = styled.li`
  display: flex;
  align-items: center;
  margin-bottom: ${props => props.theme.spacing.medium};
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.medium};

  &:before {
    content: "•";
    color: ${props => props.theme.colors.primary};
    font-weight: bold;
    margin-right: ${props => props.theme.spacing.medium};
  }
`;

const ControlsGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: ${props => props.theme.spacing.large};
  margin-top: ${props => props.theme.spacing.large};
`;

const ControlCard = styled.div`
  background: ${props => props.theme.colors.background};
  padding: ${props => props.theme.spacing.large};
  border-radius: ${props => props.theme.spacing.small};
  box-shadow: ${props => props.theme.shadows.small};
`;

const ControlTitle = styled.h4`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.large};
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const KeyBinding = styled.div`
  display: flex;
  align-items: center;
  margin-bottom: ${props => props.theme.spacing.small};
`;

const Key = styled.span`
  background: ${props => props.theme.colors.surface};
  padding: ${props => props.theme.spacing.small} ${props => props.theme.spacing.medium};
  border-radius: ${props => props.theme.spacing.small};
  margin-right: ${props => props.theme.spacing.medium};
  font-family: monospace;
  font-weight: bold;
`;

const ContactInfo = styled.div`
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: ${props => props.theme.spacing.large};
  margin-top: ${props => props.theme.spacing.large};
`;

const ContactItem = styled.div`
  text-align: center;
  padding: ${props => props.theme.spacing.large};
  background: ${props => props.theme.colors.background};
  border-radius: ${props => props.theme.spacing.small};
  transition: transform 0.3s ease;

  &:hover {
    transform: translateY(-5px);
  }
`;

const ContactTitle = styled.h4`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.large};
  margin-bottom: ${props => props.theme.spacing.medium};
`;

const ContactLink = styled.a`
  color: ${props => props.theme.colors.primary};
  text-decoration: none;
  font-size: ${props => props.theme.fontSizes.medium};

  &:hover {
    text-decoration: underline;
  }
`;

const FAQSection = styled.div`
  margin-top: ${props => props.theme.spacing.xlarge};
  margin-bottom: ${props => props.theme.spacing.xlarge};
  display: flex;
  flex-direction: column;
  align-items: center;
`;

const FAQList = styled.div`
  width: 100%;
  min-width: 400px;
  max-width: 600px;
  display: flex;
  flex-direction: column;
  align-items: center;
`;

const FAQItemCard = styled.div`
  background: ${props => props.theme.colors.surface};
  border-radius: ${props => props.theme.spacing.medium};
  box-shadow: 0 2px 12px rgba(0,0,0,0.10);
  margin-bottom: ${props => props.theme.spacing.large};
  transition: box-shadow 0.2s;
  overflow: hidden;
  cursor: pointer;
  border: 1px solid ${props => props.theme.colors.border};

  &:hover {
    box-shadow: 0 4px 24px rgba(0,0,0,0.16);
  }
`;

const FAQHeader = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: ${props => props.theme.spacing.large};
`;

const Question = styled.h4`
  color: ${props => props.theme.colors.primary};
  font-size: ${props => props.theme.fontSizes.large};
  margin: 0;
`;

const Arrow = styled.span`
  display: inline-block;
  font-size: 1.5em;
  transition: transform 0.3s;
  transform: rotate(${props => (props.open ? 90 : 0)}deg);
  color: ${props => props.theme.colors.primary};
`;

const AnswerWrapper = styled.div`
  max-height: ${props => (props.open ? '500px' : '0')};
  overflow: hidden;
  transition: max-height 0.4s cubic-bezier(0.4, 0, 0.2, 1);
  background: ${props => props.theme.colors.surface};
`;

const Answer = styled.p`
  color: ${props => props.theme.colors.text};
  font-size: ${props => props.theme.fontSizes.medium};
  margin: 0;
  padding: ${props => props.theme.spacing.large};
  padding-top: 0;
`;

const UpdatesSection = styled.section`
  padding: ${props => props.theme.spacing.xxlarge} 0;
  background: ${props => props.theme.colors.surface};
  margin-top: ${props => props.theme.spacing.xlarge};
`;

const AnchorNav = styled.div`
  display: flex;
  justify-content: center;
  gap: ${props => props.theme.spacing.large};
  margin-bottom: ${props => props.theme.spacing.xlarge};
`;

const AnchorButton = styled.button`
  background: ${props => props.theme.colors.gradient};
  color: white;
  border: none;
  border-radius: ${props => props.theme.spacing.small};
  padding: ${props => props.theme.spacing.small} ${props => props.theme.spacing.large};
  font-size: ${props => props.theme.fontSizes.medium};
  font-weight: 600;
  cursor: pointer;
  transition: box-shadow 0.2s, transform 0.2s;
  box-shadow: 0 2px 8px rgba(0,0,0,0.08);
  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 16px rgba(0,0,0,0.12);
  }
`;

const ComingSoon = styled.span`
  display: inline-block;
  background: ${props => props.theme.colors.warning};
  color: #222;
  font-weight: 700;
  font-size: 0.9em;
  border-radius: 8px;
  padding: 2px 10px;
  margin-left: 12px;
  vertical-align: middle;
`;

const HEADER_OFFSET = 80; // Adjust this value to match your header height

function InfoPage(props) {
  const topRef = useRef(null);
  const howToPlayRef = useRef(null);
  const plannedUpdateRef = useRef(null);
  const updatesRef = useRef(null);
  const gameFeaturesRef = useRef(null);
  const termsRef = useRef(null);
  const faqRef = useRef(null);
  const supportRef = useRef(null);
  const faqs = [
    {
      q: 'How do I control my snake?',
      a: "Move your mouse to control the snake's direction. Use the right mouse button (RMB) to boost speed. On the main menu, press Tab to choose your skin."
    },
    {
      q: 'How do I unlock new skins?',
      a: 'Earn points by playing and completing challenges. Some skins may be unlocked during special events or by reaching certain milestones.'
    },
    {
      q: 'Can I play with friends?',
      a: 'Yes! You can join the same room or server as your friends to play together. Custom rooms may be available in future updates.'
    },
    {
      q: 'Is Snake.io free to play?',
      a: 'Yes, Snake.io is completely free to play. Some cosmetic items may be available for purchase or as rewards.'
    }
  ];

  const scrollToSection = ref => {
    if (ref && ref.current) {
      const top = ref.current.getBoundingClientRect().top + window.pageYOffset - HEADER_OFFSET;
      window.scrollTo({ top, behavior: 'smooth' });
    }
  };

  useEffect(() => {
    if (props.setSectionRefs) {
      props.setSectionRefs({
        topRef,
        howToPlayRef,
        plannedUpdateRef,
        updatesRef,
        gameFeaturesRef,
        termsRef,
        faqRef,
        supportRef,
        scrollToSection
      });
    }
  }, [props.setSectionRefs, topRef, howToPlayRef, plannedUpdateRef, updatesRef, gameFeaturesRef, termsRef, faqRef, supportRef]);

  useEffect(() => {
    if (props.pendingScrollSection && props.setPendingScrollSection) {
      const sectionMap = {
        topRef,
        howToPlayRef,
        plannedUpdateRef,
        updatesRef,
        gameFeaturesRef,
        termsRef,
        faqRef,
        supportRef
      };
      const ref = sectionMap[props.pendingScrollSection];
      if (ref && ref.current) {
        requestAnimationFrame(() => {
          scrollToSection(ref);
          props.setPendingScrollSection(null);
        });
      }
    }
  }, [props.pendingScrollSection, props.setPendingScrollSection, topRef, howToPlayRef, plannedUpdateRef, updatesRef, gameFeaturesRef, termsRef, faqRef, supportRef]);

  return (
    <InfoContainer>
      <div ref={topRef} />
      <AnchorNav>
        <AnchorButton onClick={() => scrollToSection(howToPlayRef)}>How to Play</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(plannedUpdateRef)}>Planned Updates</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(updatesRef)}>Updates</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(gameFeaturesRef)}>Game Features</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(termsRef)}>Terms Of Service</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(faqRef)}>FAQ</AnchorButton>
        <AnchorButton onClick={() => scrollToSection(supportRef)}>Support & Contacts</AnchorButton>
      </AnchorNav>
      <PlayerCount />
      <Section ref={howToPlayRef}>
        <SectionTitle>How to Play Snake.io</SectionTitle>
        <SubSection>
          <SubTitle>Game Overview</SubTitle>
          <Text>
            Snake.io is a multiplayer snake game where you compete against players worldwide. 
            Grow your snake by collecting food, avoid collisions, and try to become the longest snake on the server!
          </Text>
        </SubSection>

        <SubSection>
          <SubTitle>Controls</SubTitle>
          <ControlsGrid>
            <ControlCard>
              <ControlTitle>Movement</ControlTitle>
              <KeyBinding>
                <Key>Mouse</Key> - Move your mouse to control the snake's direction
              </KeyBinding>
            </ControlCard>
            <ControlCard>
              <ControlTitle>Game Actions</ControlTitle>
              <KeyBinding>
                <Key>RMB</Key> - Boost Speed
              </KeyBinding>
              <KeyBinding>
                <Key>Tab</Key> - Choose Skin (on main menu)
              </KeyBinding>
            </ControlCard>
          </ControlsGrid>
        </SubSection>

        <SubSection>
          <SubTitle>Game Objectives</SubTitle>
          <FeatureList>
            <FeatureItem>Collect food to grow your snake longer</FeatureItem>
            <FeatureItem>Avoid collisions with other snakes and walls</FeatureItem>
            <FeatureItem>Eat smaller snakes to grow even faster</FeatureItem>
            <FeatureItem>Use boost strategically to escape or catch other snakes</FeatureItem>
            <FeatureItem>Compete for the highest score on the leaderboard</FeatureItem>
          </FeatureList>
        </SubSection>

        <SubSection>
          <SubTitle>Tips for Success</SubTitle>
          <FeatureList>
            <FeatureItem>Start by collecting food in less crowded areas</FeatureItem>
            <FeatureItem>Use boost sparingly as it makes you more vulnerable</FeatureItem>
            <FeatureItem>Watch out for larger snakes and plan escape routes</FeatureItem>
            <FeatureItem>Team up with other players for protection</FeatureItem>
            <FeatureItem>Learn to predict other players' movements</FeatureItem>
          </FeatureList>
        </SubSection>
      </Section>

      <Section ref={plannedUpdateRef}>
        <SectionTitle>Planned Updates</SectionTitle>
        <PlannedUpdate />
      </Section>

      <Section ref={updatesRef}>
        <SectionTitle>Latest Updates</SectionTitle>
        <Updates />
      </Section>

      <Section ref={gameFeaturesRef}>
        <SectionTitle>Game Features</SectionTitle>
        <SubSection>
          <SubTitle>Customization</SubTitle>
          <FeatureList>
            <FeatureItem>Unlock unique snake skins and patterns</FeatureItem>
            <FeatureItem>Customize your snake's colors and effects</FeatureItem>
            <FeatureItem>Special skins for achievements and events</FeatureItem>
            <FeatureItem>Personalize your player profile</FeatureItem>
          </FeatureList>
        </SubSection>
      </Section>

      <Section ref={termsRef}>
        <SectionTitle>Terms of Service</SectionTitle>
        <SubSection>
          <SubTitle>Game Rules</SubTitle>
          <Text>
            To ensure a fair and enjoyable gaming experience for all players, please follow these rules:
          </Text>
          <FeatureList>
            <FeatureItem>No cheating or using unauthorized modifications</FeatureItem>
            <FeatureItem>Respect other players and maintain good sportsmanship</FeatureItem>
            <FeatureItem>No inappropriate usernames or chat messages</FeatureItem>
            <FeatureItem>Report bugs and issues through official channels</FeatureItem>
          </FeatureList>
        </SubSection>

        <SubSection>
          <SubTitle>Account Guidelines</SubTitle>
          <Text>
            Your account is your responsibility. Keep it secure and follow these guidelines:
          </Text>
          <FeatureList>
            <FeatureItem>Use a strong, unique password</FeatureItem>
            <FeatureItem>Don't share your account with others</FeatureItem>
            <FeatureItem>Keep your email address up to date</FeatureItem>
            <FeatureItem>Report any suspicious activity immediately</FeatureItem>
          </FeatureList>
        </SubSection>
      </Section>

      <FAQSection ref={faqRef}>
        <SectionTitle>Frequently Asked Questions</SectionTitle>
        <FAQList>
          {faqs.map((item, idx) => (
            <FAQItemCard key={idx}>
              <FAQHeader>
                <Question>{item.q}</Question>
                <Arrow open={true}>▶</Arrow>
              </FAQHeader>
              <AnswerWrapper open={true}>
                <Answer>{item.a}</Answer>
              </AnswerWrapper>
            </FAQItemCard>
          ))}
        </FAQList>
      </FAQSection>

      <Section ref={supportRef}>
        <SectionTitle>Support & Contacts</SectionTitle>
        <Text>
          Join our growing community of players! Get help, share strategies, and connect with other
          Snake.io enthusiasts. Our support team is always ready to assist you.
        </Text>
        <ContactInfo>
          <ContactItem>
            <ContactTitle>Discord Community</ContactTitle>
            <ContactLink href="https://discord.gg/snakeio" target="_blank" rel="noopener noreferrer">
              Join our Discord
            </ContactLink>
          </ContactItem>
          <ContactItem>
            <ContactTitle>Email Support</ContactTitle>
            <ContactLink href="mailto:support@snakeio.com">
              support@snakeio.com
            </ContactLink>
          </ContactItem>
          <ContactItem>
            <ContactTitle>Twitter</ContactTitle>
            <ContactLink href="https://twitter.com/snakeio" target="_blank" rel="noopener noreferrer">
              @snakeio
            </ContactLink>
          </ContactItem>
        </ContactInfo>
      </Section>
    </InfoContainer>
  );
}

export default InfoPage; 