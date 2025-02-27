private void ShowPrizeWon(Prize prize)
    {
        if (prize.name == "Random Era Unlocked" || prize.name == "Try Again")
        {
            prizetext.gameObject.SetActive(true);
            prizeWonPanel.gameObject.SetActive(true);
            prizetext.text = $"{prize.prizeName}!";
        }
    }